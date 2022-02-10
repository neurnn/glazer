using Backrole.Crypto;
using Glazer.Core.Helpers;
using System;
using System.IO;
using System.Linq;

namespace glkey
{
    class Program
    {
        static int Main(string[] Args)
        {
            if (!Args.Contains("--no-banner"))
            {
                Console.WriteLine("Glazer Key-Pair Generator.");
                Console.WriteLine("copyright (c) 2022 neurnn corp, all rights reserved.");
            }

            if (Args.Contains("--help"))
            {
                Console.WriteLine(
                    "usage: glkey [--no-banner][--to-file FILE]\n" +
                    "" +
                    "e.g. glkey --no-banner --to-file mykey.txt\n" +
                    "e.g. glkey --no-banner (STDOUT)");

                return 0;
            }

            var Secp = Signs.Default.Get("SECP256K1");
            var KeyPair = Secp.MakeKeyPair(true);

            if (Args.Contains("--to-file"))
            {
                var Name = Args.FirstOrDefault(X => !X.StartsWith("--"));
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Console.WriteLine("--to-file option requires file name.");
                    return 1;
                }

                File.WriteAllText(Name,
                    $"{KeyPair.PublicKey.ToBase58PublicKey()}\n" +
                    $"{KeyPair.PrivateKey.ToBase58PrivateKey()}");
                return 0;
            }

            Console.WriteLine($"Public Key: {KeyPair.PublicKey.ToBase58PublicKey()}");
            Console.WriteLine($"Private Key: {KeyPair.PrivateKey.ToBase58PrivateKey()}");
            return 0;
        }
    }
}
