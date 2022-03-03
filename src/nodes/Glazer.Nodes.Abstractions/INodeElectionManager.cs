using System;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeElectionManager
    {
        /// <summary>
        /// Get all currently active voting subjects.
        /// </summary>
        /// <returns></returns>
        INodeElectionVote[] GetCurrentVotes();

        /// <summary>
        /// Issue a new vote subject.
        /// </summary>
        /// <param name="Subject"></param>
        /// <param name="Data"></param>
        /// <param name="Duration">duration to wait votes.</param>
        /// <returns></returns>
        INodeElectionVote Issue(string Subject, byte[] Data, long Duration);

        /// <summary>
        /// Subscribes the election votes.
        /// </summary>
        /// <param name="Subscriber"></param>
        /// <returns></returns>
        IDisposable Subscribe(Func<INodeElectionVote, Task> Subscriber);
    }
}
