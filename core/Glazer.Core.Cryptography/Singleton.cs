
namespace Glazer.Core.Cryptography
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        /// <summary>
        /// Instance.
        /// </summary>
        public static readonly T Instance = new();
    }
}
