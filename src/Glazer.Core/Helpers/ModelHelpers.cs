using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Helpers
{
    public static class ModelHelpers
    {
        /// <summary>
        /// Ensures the <paramref name="Field"/> kept as not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <returns></returns>
        public static T Ensures<T>(ref T Field) where T : new()
        {
            if (Field is null)
                Field = new T();

            return Field;
        }

        /// <summary>
        /// Ensures the <paramref name="Field"/> kept as not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <returns></returns>
        public static T Ensures<T>(ref T Field, Action<T> Action) where T : new()
        {
            if (Field is null)
                Action(Field = new T());

            return Field;
        }

        /// <summary>
        /// Assigns the <paramref name="Input"/> to <paramref name="Field"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <param name="Input"></param>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static T Assigns<T>(ref T Field, T Input)
        {
            if (!Field.Equals(Input))
                Field = Input;

            return Input;
        }

        /// <summary>
        /// Assigns the <paramref name="Input"/> to <paramref name="Field"/> and then,
        /// Invokes the action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <param name="Input"></param>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static T Assigns<T>(ref T Field, T Input, Action<T> Action)
        {
            if (!Field.Equals(Input))
                Action(Field = Input);

            return Input;
        }

        /// <summary>
        /// Initiate the nullable field as.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <param name="Initiator"></param>
        /// <returns></returns>
        public static T Initiate<T>(ref T? Field, Func<T> Initiator) where T : struct
        {
            if (Field.HasValue)
                return Field.Value;

            return (Field = Initiator()).Value;
        }

        /// <summary>
        /// Initiate the nullable field as.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Field"></param>
        /// <param name="Initiator"></param>
        /// <returns></returns>
        public static T OnDemand<T>(ref T Field, Func<T> Initiator) where T : class
        {
            if (Field != null)
                return Field;

            return (Field = Initiator());
        }
    }
}
