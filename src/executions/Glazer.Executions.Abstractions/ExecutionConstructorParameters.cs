using Glazer.Accessors;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;

namespace Glazer.Executions.Abstractions
{
    internal struct ExecutionConstructorParameters
    {
        public Actor AuthorizedActor { get; set; }

        public ScriptAction TargetAction { get; set; }

        public ProtocolAbi Contract { get; set; }

        public IKvTable ScopedKvTable { get; set; }

        public ExecutionContextFactory Factory { get; set; }
    }
}
