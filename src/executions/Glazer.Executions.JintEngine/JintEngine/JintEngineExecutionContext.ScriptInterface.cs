using Glazer.Common.Models;
using Glazer.Executions.Abstractions;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Jint.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Glazer.Executions.JintEngine
{
    public partial class JintEngineExecutionContext
    {
        private class ScriptInterface
        {
            private JintEngineExecutionContext m_Context;
            private IKvTable m_Table;

            /// <summary>
            /// Initialize the <see cref="ScriptInterface"/> instance.
            /// </summary>
            /// <param name="Context"></param>
            public ScriptInterface(JintEngineExecutionContext Context, IKvTable Table)
            {
                m_Context = Context;
                m_Table = Table;
            }

            /// <summary>
            /// Get the actor that requested the execution.
            /// </summary>
            /// <returns></returns>
            public string get_actor() => m_Context.AuthorizedActor;

            /// <summary>
            /// Get the action that requested.
            /// </summary>
            /// <returns></returns>
            public string get_action() => m_Context.TargetAction.Action;

            /// <summary>
            /// Get the ID of the script.
            /// </summary>
            /// <returns></returns>
            public string get_script_id() => m_Context.TargetAction.Script.ToString();

            /// <summary>
            /// Get the owner of the script action.
            /// </summary>
            /// <returns></returns>
            public string get_script_owner() => m_Context.TargetActionOwner;

            /// <summary>
            /// Get the value by its key.
            /// </summary>
            /// <param name="Key"></param>
            /// <returns></returns>
            public JsValue get(string Key)
            {
                var Bson = m_Table.GetBsonObject(Key);
                if (Bson is not null)
                {
                    return m_Context.m_Engine
                        .SetValue("__temp", Bson.ToString(Formatting.None))
                        .Execute("JSON.parse(__temp)")
                        .GetCompletionValue();
                }

                return JsValue.Null;
            }

            /// <summary>
            /// Set the value by its key.
            /// </summary>
            /// <param name="Key"></param>
            /// <param name="Value"></param>
            /// <returns></returns>
            public ScriptInterface set(string Key, JsValue Value)
            {
                if (string.IsNullOrWhiteSpace(Key))
                    throw new ArgumentException("No empty key allowed.");

                if (Value is null || !Value.IsObject())
                    throw new InvalidOperationException($"Non-object value can not be set for: {Key}.");

                var JsonStr = m_Context.m_Engine
                    .SetValue("__temp", Value)
                    .Execute("JSON.stringify(__temp)")
                    .GetCompletionValue().AsString();

                var Json = JsonConvert.DeserializeObject<JObject>(JsonStr);
                if (!m_Table.SetBsonObject(Key, Json))
                    throw new IOException($"Failed to set the value for the key, {Key}: {JsonStr}.");

                return this;
            }

            /// <summary>
            /// Execute the other script action with arguments.
            /// </summary>
            /// <param name="ScriptId"></param>
            /// <param name="Action"></param>
            /// <param name="Arguments"></param>
            /// <returns></returns>
            public ExecutionResult exec(string ScriptId, string Action, JsValue Arguments)
            {
                if (string.IsNullOrWhiteSpace(ScriptId) || Guid.TryParse(ScriptId, out var Id))
                {
                    return new ExecutionResult
                    {
                        Succeed = false,
                        Reason = $"the script id is invalid: {ScriptId ?? "(null)"}."
                    };
                }

                if (string.IsNullOrWhiteSpace(Action))
                {
                    return new ExecutionResult
                    {
                        Succeed = false,
                        Reason = $"the action name is invalid: {Action ?? "(null)"}."
                    };
                }

                if (Arguments is null || !Arguments.IsArray())
                {
                    return new ExecutionResult
                    {
                        Succeed = false,
                        Reason = "the arguments should be array."
                    };
                }
                try
                {
                    var JsonStr = m_Context.m_Engine
                        .SetValue("__temp", Arguments).Execute("JSON.stringify(__temp)")
                        .GetCompletionValue().AsString();

                    var Json = JsonConvert.DeserializeObject<JArray>(JsonStr);
                    var Target = new ScriptAction(new ScriptId(Id), Action);

                    return m_Context
                        .ExecuteOtherAsync(Target, Json)
                        .GetAwaiter().GetResult();
                }

                catch(Exception e)
                {
                    return new ExecutionResult
                    {
                        Succeed = false,
                        Reason = e.Message
                    };
                }
            }
        }
    }
}
