using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages
{
    public static class StorageHelpers
    {
        /// <summary>
        /// Normalize the path string to plain path.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string Normalize(string Key)
        {
            var Elems = new Queue<string>(Key.Split('/', StringSplitOptions.RemoveEmptyEntries));
            var Output = new List<string>();

            while (Elems.TryDequeue(out var Name))
            {
                switch (Name)
                {
                    case "": case ".": continue;
                    case "..":
                        if (Output.Count > 0)
                            Output.RemoveAt(Output.Count - 1);

                        continue;

                    default:
                        break;
                }

                Output.Add(Name);
            }

            Key = string.Join('/', Output);
            return Key;
        }
    }
}
