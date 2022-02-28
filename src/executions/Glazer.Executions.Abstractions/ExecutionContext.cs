using Glazer.Accessors;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Glazer.Kvdb.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Glazer.Executions.Abstractions.ExecutionStatus;

namespace Glazer.Executions.Abstractions
{
    public abstract class ExecutionContext
    {
        private static readonly ScriptAction[] EMPTY_ACTIONS = new ScriptAction[0];
        private static readonly string[] EMPTY_STRINGS = new string[0];
        private ExecutionContextFactory m_ContextFactory;

        private ProtocolAbi m_Contract;
        private IKvTable m_ScopedKvTable;

        /// <summary>
        /// Initialize a new <see cref="ExecutionContext"/> instance.
        /// </summary>
        protected ExecutionContext()
        {
            var Parameters = GetCurrentParameters();
            if ((m_ContextFactory = Parameters.Factory) is null)
                throw new InvalidOperationException("No construction time of the execution context.");

            AuthorizedActor = Parameters.AuthorizedActor;
            TargetAction = Parameters.TargetAction;

            m_Contract = Parameters.Contract;
            m_ScopedKvTable = Parameters.ScopedKvTable;
        }

        /// <summary>
        /// Authorized Actor of the context.
        /// </summary>
        public Actor AuthorizedActor { get; }

        /// <summary>
        /// Script Owner of the 
        /// </summary>
        public Actor TargetActionOwner => m_Contract.Owner;

        /// <summary>
        /// Target Action to execute.
        /// </summary>
        public ScriptAction TargetAction { get; }

        /// <summary>
        /// Reset the execution context that loaded the <paramref name="ScriptAbi"/>.
        /// </summary>
        protected abstract Task OnResetAsync(IKvTable Table, byte[] ScriptAbi);

        /// <summary>
        /// Get all actions of the <see cref="ScriptAction.Action"/>.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ScriptAction[]> GetActionsAsync(CancellationToken Token = default)
        {
            try
            {
                var ScriptAbi = Convert.FromBase64String(m_Contract.Data);
                if (ScriptAbi is null)
                {
                    return EMPTY_ACTIONS;
                }

                using var Temp = new MemoryKvTable();
                await OnResetAsync(Temp, ScriptAbi);

                return (await OnGetActionsAsync(Token) ?? EMPTY_STRINGS)
                    .Select(X => new ScriptAction(TargetAction.Script, X))
                    .ToArray();
            }
            catch(Exception)
            {
                return EMPTY_ACTIONS;
            }
        }

        /// <summary>
        /// Execute the <see cref="ScriptAction"/> with given arguments.
        /// </summary>
        /// <param name="Arguments"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ExecutionResult> ExecuteAsync(JArray Arguments, CancellationToken Token = default)
        {
            var UpperCaptures = GetCurrentContextKvTable();
            var Capture = new MemoryKvTable();

            PushCurrentContext(this, Capture);
            try
            {
                var DataSet = m_ScopedKvTable;
                if (UpperCaptures != null)
                    DataSet = DataSet.Overlay(UpperCaptures);

                var Table = DataSet.Duplex(Capture.Prefix($"{TargetAction.Script}:"));

                try
                {
                    var ScriptAbi = Convert.FromBase64String(m_Contract.Data);
                    if (ScriptAbi is null)
                    {
                        return new ExecutionResult
                        {
                            Succeed = false,
                            Reason = "No script abi loaded."
                        };
                    }

                    await OnResetAsync(Table, ScriptAbi);
                    await OnExecuteAsync(Table, Arguments, Token);
                }
                catch (Exception e)
                {
                    return new ExecutionResult
                    {
                        Succeed = false,
                        Reason = e.Message
                    };
                }

                foreach (var Each in Capture)
                    UpperCaptures.Set(Each.Key, Each.Value);

                return new ExecutionResult
                {
                    Succeed = true, Reason = string.Empty,
                    Outputs = Capture.ToReadOnly()
                };
            }

            finally { PopCurrentContext(); }
        }

        /// <summary>
        /// Execute the other contract with arguments asynchronously.
        /// </summary>
        /// <param name="Action"></param>
        /// <param name="Arguments"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected async Task<ExecutionResult> ExecuteOtherAsync(ScriptAction Action, JArray Arguments, CancellationToken Token = default)
        {
            ExecutionContext Context;

            try { Context = await m_ContextFactory.CreateAsync(AuthorizedActor, Action, Token); }
            catch(Exception e)
            {
                return new ExecutionResult
                {
                    Succeed = false,
                    Reason = e.Message
                };
            }

            return await Context.ExecuteAsync(Arguments, Token);
        }

        /// <summary>
        /// Called to get the contract actions asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task<string[]> OnGetActionsAsync(CancellationToken Token = default);

        /// <summary>
        /// Called to execute the contract asynchronously.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Arguments"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task OnExecuteAsync(IKvTable Table, JArray Arguments, CancellationToken Token = default);
    }
}
