using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Common;
using Glazer.Common.Models;
using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Protocols
{
    public sealed partial class ElectionVote
    {
        /// <summary>
        /// Election Session Manager.
        /// </summary>
        internal class Manager : INodeElectionManager, IDisposable
        {
            private ILogger m_Logger;
            private IMessanger m_Messanger;

            private List<Session> m_Sessions = new();
            private NodeOptions m_Options;
            private SignKeyPair m_KeyPair;

            private CancellationTokenSource m_Cts;
            private Task m_Timer;

            private List<Func<INodeElectionVote, Task>> m_Subscribers;

            /// <summary>
            /// Initialize a new <see cref="Manager"/> instance.
            /// </summary>
            /// <param name="Services"></param>
            public Manager(IServiceProvider Services, ILogger<ElectionVote> Logger)
            {
                m_Logger = Logger; m_Options = Services.GetRequiredService<NodeOptions>();
                m_Messanger = Services.GetRequiredService<IMessanger>();
                m_Subscribers = new List<Func<INodeElectionVote, Task>>();
                m_KeyPair = m_Options.GetKeyPair();
                m_Cts = new CancellationTokenSource();
            }

            /// <inheritdoc/>
            public INodeElectionVote[] GetCurrentVotes()
            {
                lock(m_Sessions)
                {
                    return m_Sessions.ToArray();
                }
            }

            /// <inheritdoc/>
            public IDisposable Subscribe(Func<INodeElectionVote, Task> Subscriber)
            {
                lock (m_Subscribers)
                {
                    m_Subscribers.Add(Subscriber);
                    return Disposables.FromAction(() =>
                    {
                        lock (m_Subscribers)
                              m_Subscribers.Remove(Subscriber);
                    });
                }
            }

            /// <summary>
            /// Notify the <see cref="INodeElectionVote"/> to subscribers.
            /// </summary>
            /// <param name="Voting"></param>
            /// <returns></returns>
            private async Task NotifyToSubscribers(INodeElectionVote Voting)
            {
                Func<INodeElectionVote, Task>[] Subscribers;

                lock (m_Subscribers)
                {
                    if (m_Subscribers.Count <= 0)
                        return;

                    Subscribers = m_Subscribers.ToArray();
                }

                foreach(var Each in Subscribers)
                    await Each(Voting);
            }

            /// <inheritdoc/>
            public INodeElectionVote Issue(string Subject, byte[] Data, long Duration)
            {
                var Organizer = new WitnessActor(m_Options.Actor, m_KeyPair.SignSeal(Data ??= Session.EMPTY_EVIDENCE));
                var Voting = new Session(m_Messanger, Organizer, Subject, TimeStamp.Now + Math.Max(Duration, 1), Data);

                lock (m_Sessions)
                      m_Sessions.Add(Voting);

                using (var Writer = new PacketWriter())
                {
                    Writer.Write(Organizer);
                    Writer.Write(Subject);

                    Writer.Write7BitEncodedInt(Voting.Data.Length);
                    Writer.Write(Voting.Data);

                    m_Messanger.Emit(new Message
                    {
                        Headers = new Dictionary<string, string>()
                        {
                            { "Type", MESSAGE_TYPE },
                            { "Subtype", "issue"   }
                        },

                        Expiration = Voting.Expiration,
                        Data = Writer.ToByteArray()
                    });
                }

                SpawnTimer();
                return Voting;
            }

            /// <summary>
            /// Dispose the <see cref="Manager"/> instance.
            /// </summary>
            public void Dispose()
            {
                m_Cts.Cancel();

                Task Waits;
                lock (this)
                    Waits = m_Timer;

                if (Waits is not null)
                    Waits.GetAwaiter().GetResult();

                while (true)
                {
                    Session[] Snapshot;

                    lock (m_Sessions)
                    {
                        if (m_Sessions.Count <= 0)
                            break;

                        Snapshot = m_Sessions.ToArray();
                        m_Sessions.Clear();
                    }

                    foreach (var Each in Snapshot)
                    {
                        Each.OnCanceled();
                    }
                }

                m_Cts.Dispose();
            }

            /// <summary>
            /// Handle the message.
            /// </summary>
            /// <param name="Message"></param>
            /// <returns></returns>
            public void OnMessage(Message Message)
            {
                if (!Message.Headers.TryGetValue("Subtype", out var Subtype))
                    return;

                if (string.IsNullOrWhiteSpace(Subtype))
                    return;

                if (Message.Data is null || Message.Data.Length <= 0)
                    return;

                if (Subtype != "issue" && Subtype != "vote")
                    return;

                using (var Reader = new PacketReader(Message.Data))
                {
                    var Organizer = Reader.ReadWitnessActor();
                    var Subject = Reader.ReadString();

                    var Length = Reader.Read7BitEncodedInt();
                    var Evidence = Reader.ReadBytes(Length); // --> Data if it is issueing.

                    if (!Organizer.IsValid || string.IsNullOrWhiteSpace(Subject) ||
                        !Organizer.Signature.Verify(Evidence))
                    {
                        return;
                    }

                    switch (Subtype)
                    {
                        case "issue":
                            {
                                var Session = GetOrCreateSession(
                                    Message, Organizer, Subject, Evidence,
                                    out var IsCreatedNow);

                                if (IsCreatedNow)
                                {
                                    m_Logger.LogInformation(
                                        $"New vote-subject received: {Subject} ({Organizer.Signature}) by {Organizer.Actor}");

                                    _ = NotifyToSubscribers(Session);
                                    SpawnTimer();
                                }
                            }
                            break;

                        case "vote":
                            {
                                var Session = GetSession(Organizer, Subject);
                                if (Session is null)
                                    return;

                                // Apply the vote to the session.
                                Session.OnVote(Reader.ReadWitnessActor(), Evidence);
                            }
                            break;
                    }
                }
            }

            /// <summary>
            /// Get or create a voting session.
            /// </summary>
            /// <param name="Message"></param>
            /// <param name="Organizer"></param>
            /// <param name="Subject"></param>
            /// <param name="Evidence"></param>
            /// <param name="IsCreatedNow"></param>
            /// <returns></returns>
            private Session GetOrCreateSession(Message Message, WitnessActor Organizer, string Subject, byte[] Evidence, out bool IsCreatedNow)
            {
                Session Session;

                lock (m_Sessions)
                {
                    if ((Session = GetSession(Organizer, Subject)) is null)
                    {
                        m_Sessions.Add(Session = new Session(m_Messanger, Organizer, Subject, Message.Expiration, Evidence));
                        IsCreatedNow = true;
                    }

                    else IsCreatedNow = false;
                }

                return Session;
            }

            /// <summary>
            /// Get the session if exists.
            /// </summary>
            /// <param name="Organizer"></param>
            /// <param name="Subject"></param>
            /// <returns></returns>
            private Session GetSession(WitnessActor Organizer, string Subject)
            {
                lock (m_Sessions)
                {
                    return m_Sessions.FirstOrDefault(
                        X => X.Organizer == Organizer && X.Subject == Subject);
                }
            }

            private void SpawnTimer()
            {
                lock(this)
                {
                    if (m_Timer is null || m_Timer.IsCompleted)
                        m_Timer = RunTimer(m_Cts.Token);
                }
            }

            /// <summary>
            /// Run the election vote timer.
            /// </summary>
            /// <param name="Token"></param>
            /// <returns></returns>
            private async Task RunTimer(CancellationToken Token)
            {
                var Before = DateTime.Now;
                var Queue = new Queue<Session>();

                while (true)
                {
                    var Span = DateTime.Now - Before;
                    if (Span.TotalSeconds < 1)
                    {
                        try { await Task.Delay(TimeSpan.FromSeconds(1) - Span, Token); }
                        catch
                        {
                            break;
                        }

                        continue;
                    }

                    var Origin = TimeStamp.Now;
                    lock (m_Sessions)
                    {
                        var Expired = m_Sessions.Where(X => X.Expiration <= Origin);
                        foreach (var Each in Expired)
                        {
                            Queue.Enqueue(Each);
                        }
                    }

                    while (Queue.TryDequeue(out var Session))
                    {
                        Session.OnCompleted();

                        lock(m_Sessions)
                             m_Sessions.Remove(Session);
                    }

                    Before = DateTime.Now;
                }
            }
        }
    }
}
