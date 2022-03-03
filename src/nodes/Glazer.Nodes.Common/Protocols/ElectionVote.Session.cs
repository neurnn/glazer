using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Common;
using Glazer.Common.Models;
using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Protocols
{
    public partial class ElectionVote
    {
        internal sealed partial class Session : INodeElectionVote
        {
            internal static readonly byte[] EMPTY_EVIDENCE = new byte[0];
            private List<Vote> m_Votes = new();
            private TaskCompletionSource m_Tcs = new();
            private Summary m_Summary;

            /// <summary>
            /// Initialize a new <see cref="Session"/> instance.
            /// </summary>
            /// <param name="Organizer"></param>
            /// <param name="Subject"></param>
            internal Session(
                IMessanger Messanger,
                WitnessActor Organizer, string Subject,
                TimeStamp Expiration, byte[] Data)
            {
                this.Messanger = Messanger;
                this.Organizer = Organizer;
                this.Subject = Subject;
                this.Expiration = Expiration;
                this.Data = Data ?? EMPTY_EVIDENCE;
            }

            /// <inheritdoc/>
            public IMessanger Messanger { get; set; }

            /// <inheritdoc/>
            public WitnessActor Organizer { get;  }

            /// <inheritdoc/>
            public string Subject { get; }

            /// <inheritdoc/>
            public TimeStamp Expiration { get;  }

            /// <inheritdoc/>
            public byte[] Data { get;}

            /// <inheritdoc/>
            public bool Submit(byte[] Evidence)
            {
                var Options = Messanger.Services.GetService<NodeOptions>();
                if (Options is null)
                    return false;

                var Signature = Options.GetKeyPair().SignSeal(Evidence ??= EMPTY_EVIDENCE);
                var Voter = new WitnessActor(Options.Actor, Signature);

                return Submit(Voter, Evidence);
            }

            /// <inheritdoc/>
            public bool Submit(WitnessActor Voter, byte[] Evidence)
            {
                if (OnVote(Voter, Evidence))
                {
                    using var Writer = new PacketWriter();

                    Writer.Write(Organizer);
                    Writer.Write(Subject);

                    Writer.Write7BitEncodedInt(Evidence.Length);
                    Writer.Write(Evidence);

                    Writer.Write(Voter);

                    /* Emit the vote message. */
                    Messanger.Emit(new Message
                    {
                        Headers = new Dictionary<string, string>()
                        {
                            { "Type", MESSAGE_TYPE },
                            { "Subtype", "vote" }
                        },

                        Expiration = Expiration,
                        Data = Writer.ToByteArray()
                    });

                    return true;
                }

                return false;
            }

            /// <summary>
            /// Called when the voting session is completed.
            /// </summary>
            internal void OnCompleted()
            {
                m_Tcs.TrySetResult();
            }

            /// <summary>
            /// Called when the voting session is canceled.
            /// </summary>
            internal void OnCanceled()
            {
                // --> makes the session to be untrustable and unadoptable.
                lock (m_Votes)
                     m_Votes.Clear();

                m_Tcs.TrySetResult();
            }

            /// <summary>
            /// Collects votings.
            /// </summary>
            /// <param name="Voter"></param>
            /// <param name="Evidence"></param>
            internal bool OnVote(WitnessActor Voter, byte[] Evidence)
            {
                lock (this)
                {
                    if (m_Tcs.Task.IsCompleted)
                        return false;

                    if (m_Summary is not null)
                        return false; /* Voting result is already summarized. */
                }

                lock (m_Votes)
                {
                    if (!Voter.IsValid || !Voter.Signature.Verify(Evidence))
                        return false; // --> Signature mismatch.

                    /* This hash is only indexing purpose. (So, no security aware required) */
                    var Hash = Hashes.Default.Hash("MD5", Evidence ?? EMPTY_EVIDENCE);
                    var Vote = m_Votes.Find(X => X.Actor.Actor == Voter.Actor);
                    if (Vote is not null) // Alter the actor's vote.
                    {
                        lock(Vote)
                        {
                            Vote.Actor = Voter;
                            Vote.Hash = Hash;
                            Vote.Evidence = Evidence;
                        }
                    }

                    else
                    {
                        m_Votes.Add(Vote = new Vote
                        {
                            Actor = Voter,
                            Hash = Hash,
                            Evidence = Evidence
                        });
                    }

                    return true;
                }
            }

            /// <inheritdoc/>
            public async Task<INodeElectionSummary> SummarizeAsync(CancellationToken Token = default)
            {
                lock(this)
                {
                    if (m_Summary is not null)
                        return m_Summary;
                }

                var Tcs = new TaskCompletionSource();
                using (Token.Register(() => Tcs.TrySetCanceled()))
                {
                    await Task.WhenAny(Tcs.Task, m_Tcs.Task);
                    Token.ThrowIfCancellationRequested();
                }

                return SummarizeInternal();
            }

            /// <inheritdoc/>
            public INodeElectionSummary Summarize()
            {
                lock (this)
                {
                    if (m_Summary is not null)
                        return m_Summary;
                }

                m_Tcs.Task.GetAwaiter().GetResult();
                return SummarizeInternal();
            }

            /// <summary>
            /// Summarize the voting result.
            /// </summary>
            /// <returns></returns>
            private INodeElectionSummary SummarizeInternal()
            {
                lock(this)
                {
                    if (!m_Tcs.Task.IsCompleted)
                        throw new InvalidOperationException("the voting is not finished.");

                    if (m_Summary is not null)
                        return m_Summary;

                    lock (m_Votes)
                    {
                        return m_Summary = new Summary(this, m_Votes);
                    }
                }
            }
        }
    }
}
