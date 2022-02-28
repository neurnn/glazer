using Glazer.Kvdb.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Memory.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var Scheme = new MemoryKvScheme();

            var Table = Scheme.Open("test");
            if (Table is null)
                Table = Scheme.Create("test");

            Table.SetString("item", "value");

            Table = Scheme.Open("test");
            Debug.Assert(Table.GetString("item") == "value");
        }
    }
}
