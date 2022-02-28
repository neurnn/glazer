namespace Glazer.Accessors.Abstractions
{
    public static class IDataAccessorExtensions
    {
        /// <summary>
        /// Get a data by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static TData? GetData<TData>(this IDataAccessor<TData> This, string Key) where TData : struct
        {
            return This.GetDataAsync(Key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set a data by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static bool SetData<TData>(this IDataAccessor<TData> This, string Key, TData? Value) where TData : struct
        {
            return This.SetDataAsync(Key, Value).GetAwaiter().GetResult();
        }
    }
}
