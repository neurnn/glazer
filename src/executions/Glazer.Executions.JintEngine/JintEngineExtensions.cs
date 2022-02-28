using Glazer.Executions.Abstractions;
using Glazer.Executions.JintEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Executions
{
    public static class JintEngineExtensions
    {
        private const string NAME_ECMA_5_1 = "ecma5.1";
        private const string NAME_JINTENGINE = "jint";

        /// <summary>
        /// Set the `jint` execution engine as <see cref="Jint.Engine"/>.
        /// </summary>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static ExecutionContextFactory UseJintEngine(this ExecutionContextFactory Factory) => Factory
            .SetFactory(NAME_JINTENGINE, () => new JintEngineExecutionContext())
            .SetFactory(NAME_ECMA_5_1, () => new JintEngineExecutionContext());
    }
}
