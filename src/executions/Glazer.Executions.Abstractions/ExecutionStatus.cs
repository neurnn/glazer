using Glazer.Kvdb.Memory;
using System.Collections.Generic;
using System.Threading;

namespace Glazer.Executions.Abstractions
{

    internal static class ExecutionStatus
    {
        private static readonly AsyncLocal<Stack<ExecutionConstructorParameters>> PARAMETERS = new ();
        private static readonly AsyncLocal<Stack<(ExecutionContext Context, MemoryKvTable Capture)>> CONTEXTS = new ();

        /// <summary>
        /// Get the parameter stack.
        /// </summary>
        /// <returns></returns>
        private static Stack<ExecutionConstructorParameters> ConstructorParameters(bool CreateNew = false)
        {
            if (PARAMETERS.Value is null && CreateNew)
                PARAMETERS.Value = new Stack<ExecutionConstructorParameters>();

            return PARAMETERS.Value;
        }

        /// <summary>
        /// Get the parameter stack.
        /// </summary>
        /// <returns></returns>
        private static Stack<(ExecutionContext Context, MemoryKvTable Capture)> ExecutionContexts(bool CreateNew = false)
        {
            if (CONTEXTS.Value is null && CreateNew)
                CONTEXTS.Value = new();

            return CONTEXTS.Value;
        }

        /// <summary>
        /// Push the current constructor parameter.
        /// </summary>
        /// <param name="Actor"></param>
        public static void PushCurrentParameters(ExecutionConstructorParameters Parameters)
        {
            ConstructorParameters(true).Push(Parameters);
        }

        /// <summary>
        /// Push the current execution context.
        /// </summary>
        /// <param name="Context"></param>
        public static void PushCurrentContext(ExecutionContext Context, MemoryKvTable Capture)
        {
            ExecutionContexts(true).Push((Context, Capture));
        }

        /// <summary>
        /// Pop the current constructor parameter.
        /// </summary>
        /// <returns></returns>
        public static bool PopCurrentParameters()
        {
            return ConstructorParameters(true).TryPop(out _);
        }

        /// <summary>
        /// Pop the current execution context.
        /// </summary>
        /// <returns></returns>
        public static bool PopCurrentContext()
        {
            return ExecutionContexts(true).TryPop(out _);
        }

        /// <summary>
        /// Get the current authorized actor from the stack.
        /// </summary>
        /// <returns></returns>
        public static ExecutionConstructorParameters GetCurrentParameters()
        {
            var Stack = ConstructorParameters();
            if (Stack is null) return default;
            return Stack.Peek();
        }

        /// <summary>
        /// Get the current execution context from the stack.
        /// </summary>
        /// <returns></returns>
        public static ExecutionContext GetCurrentContext()
        {
            var Stack = ExecutionContexts();
            if (Stack is null) return default;
            return Stack.Peek().Context;
        }

        /// <summary>
        /// Get the current execution context from the stack.
        /// </summary>
        /// <returns></returns>
        public static MemoryKvTable GetCurrentContextKvTable()
        {
            var Stack = ExecutionContexts();
            if (Stack is null) return default;
            return Stack.Peek().Capture;
        }

    }
}
