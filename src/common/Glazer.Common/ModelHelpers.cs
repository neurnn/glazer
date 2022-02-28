using Newtonsoft.Json.Linq;
using System;

namespace Glazer
{
    public static class ModelHelpers
    {
        public delegate bool TryImport<TValue>(JObject Json, out TValue Value);

        /// <summary>
        /// Export a value using the exporter delegate.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Exporter"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static JObject Export<TValue>(Func<JObject, TValue, bool> Exporter, TValue Value)
        {
            var Json = new JObject();

            if (Exporter(Json, Value))
                return Json;

            throw new InvalidOperationException("Nothing exported.");
        }

        /// <summary>
        /// Import a value using the importer delegate.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Input"></param>
        /// <param name="Importer"></param>
        /// <returns></returns>
        public static TValue Import<TValue>(this JObject Input, TryImport<TValue> Importer)
        {
            
            if (Input != null && Importer(Input, out var Value))
                return Value;

            throw new InvalidOperationException("Nothing imported.");
        }

        /// <summary>
        /// Export values using the exporter delegate.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Exporter"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static JArray Export<TValue>(this TValue[] Values, Func<JObject, TValue, bool> Exporter)
        {
            var Result = new JArray();

            foreach (var Each in Values)
                Result.Add(Export(Exporter, Each));

            return Result;
        }

        /// <summary>
        /// Initialize a field as on demand.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Field"></param>
        /// <returns></returns>
        public static TValue OnDemand<TValue>(ref TValue Field) where TValue : new()
        {
            if (Field is null)
                Field = new TValue();

            return Field;
        }

        /// <summary>
        /// Initialize a field as on demand and invoke the callback when the field is initialized.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Field"></param>
        /// <param name="Callback"></param>
        /// <returns></returns>
        public static TValue OnDemand<TValue>(ref TValue Field, Action<TValue> Callback) where TValue : new()
        {
            if (Field is null)
            {
                Field = new TValue();
                Callback?.Invoke(Field);
            }

            return Field;
        }

        /// <summary>
        /// Return the value and assign the output to `out` value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Return"></param>
        /// <param name="Where"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static TReturn Return<TOut, TReturn>(TReturn Return, out TOut Where, TOut Output)
        {
            Where = Output;
            return Return;
        }

        /// <summary>
        /// Return the value and assign the output to `out` value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Return"></param>
        /// <param name="Where"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static TReturn Return<TOut, TReturn>(TReturn Return, out TOut Where)
        {
            Where = default;
            return Return;
        }

        /// <summary>
        /// Swap the field to new value and return its previous value.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Value"></param>
        /// <param name="NewValue"></param>
        /// <returns></returns>
        public static TValue Swap<TValue>(ref TValue Value, TValue NewValue)
        {
            TValue Temp = Value;
            Value = NewValue;
            return Temp;
        }

        /// <summary>
        /// Swap the field to new value and return its previous value.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Field1"></param>
        /// <param name="Field2"></param>
        /// <returns></returns>
        public static void Swap<TValue>(ref TValue Field1, ref TValue Field2)
        {
            TValue Temp = Field1;
            Field1 = Field2;
            Field2 = Field1;
        }

        /// <summary>
        /// Execute an action with `lock` keyword using <paramref name="Padlock"/>.
        /// </summary>
        /// <param name="Padlock"></param>
        /// <param name="Action"></param>
        public static void Locked(object Padlock, Action Action)
        {
            lock(Padlock)
            {
                Action?.Invoke();
            }
        }

        /// <summary>
        /// Execute an action with `lock` keyword using <paramref name="Padlock"/>.
        /// </summary>
        /// <param name="Padlock"></param>
        /// <param name="Action"></param>
        public static TReturn Locked<TReturn>(object Padlock, Func<TReturn> Action)
        {
            lock (Padlock)
            {
                return Action();
            }
        }
    }
}
