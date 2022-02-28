using Glazer.Kvdb.Abstractions;
using Jint.Native;
using Jint.Native.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExecutionContext = Glazer.Executions.Abstractions.ExecutionContext;

namespace Glazer.Executions.JintEngine
{
    /// <summary>
    /// <see cref="ExecutionContext"/> that uses <see cref="Jint.Engine"/>.
    /// </summary>
    public partial class JintEngineExecutionContext : ExecutionContext
    {
        private Jint.Engine m_Engine;

        /// <inheritdoc/>
        protected override Task OnResetAsync(IKvTable Table, byte[] ScriptAbi)
        {
            (m_Engine = new Jint.Engine())
                .SetValue("g", new ScriptInterface(this, Table))
                .SetValue("contracts", new JsValue(new ObjectInstance(m_Engine)))
                .Execute(Encoding.UTF8.GetString(ScriptAbi));

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task<string[]> OnGetActionsAsync(CancellationToken Token = default)
        {
            return Task.Run(() =>
            {
                /* Get the contract definition object. */
                var JsContracts = m_Engine.GetValue("contracts");
                if (JsContracts is null || !JsContracts.IsObject())
                    throw new InvalidOperationException("No contract valid.");

                /* Get all property names of the contract definition object. */
                return JsContracts.AsObject().GetOwnProperties().Select(X => X.Key).ToArray();
            }, Token);
        }

        /// <inheritdoc/>
        protected override Task OnExecuteAsync(IKvTable Table, JArray Arguments, CancellationToken Token = default)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(TargetAction.Action))
                    throw new InvalidOperationException("Invalid action specified.");

                /* Get the contract definition object. */
                var JsContracts = m_Engine.GetValue("contracts");
                if (JsContracts is null || !JsContracts.IsObject())
                    throw new InvalidOperationException("No contract valid.");

                /* Test whether the target action is defined on the contract object or not/ */
                var Contracts = JsContracts.AsObject();
                if (!Contracts.HasOwnProperty(TargetAction.Action))
                    throw new InvalidOperationException($"No action defined: {TargetAction.Action}.");

                /* Then, load the arguments on the Jint Engine instance. */
                var JsArguments = m_Engine
                    .SetValue("__temp", Arguments.ToString(Formatting.None))
                    .Execute("JSON.parse(__temp)").GetCompletionValue();

                /* Finally, Invoke the contract action here. */
                Contracts.Get(TargetAction.Action).Invoke(JsArguments);
            }, Token);
        }
    }
}
