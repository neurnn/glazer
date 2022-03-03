using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer
{
    public static class Disposables
    {
        private struct Disposable : IDisposable
        {
            private Action m_Action;

            /// <summary>
            /// Initialize a new <see cref="Disposable"/>.
            /// </summary>
            /// <param name="Action"></param>
            public Disposable(Action Action) => m_Action = Action;

            /// <inheritdoc/>
            public void Dispose()
            {
                m_Action?.Invoke();
                m_Action = null;
            }
        }

        /// <summary>
        /// Make an <see cref="IDisposable"/> from <see cref="Action"/> delegate.
        /// </summary>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static IDisposable FromAction(Action Action) => new Disposable(Action);
    }
}
