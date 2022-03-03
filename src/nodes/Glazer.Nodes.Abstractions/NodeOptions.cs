using Backrole.Crypto;
using Glazer.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using GActor = Glazer.Common.Models.Actor;

namespace Glazer.Nodes.Abstractions
{
    public class NodeOptions
    {
        private static readonly object[] EMPTY_ARGS = new object[0];

        /// <summary>
        /// Actor of the node.
        /// </summary>
        [JsonProperty("actor")]
        public string Actor { get; set; }

        /// <summary>
        /// Public Key of the node.
        /// </summary>
        [JsonProperty("pub_key")]
        public string PublicKey { get; set; }

        /// <summary>
        /// Private Key of the node.
        /// </summary>
        [JsonProperty("pvt_key")]
        public string PrivateKey { get; set; }

        /// <summary>
        /// Get the key pair.
        /// </summary>
        /// <returns></returns>
        public SignKeyPair GetKeyPair()
        {
            var PubKey = SignPublicKey.Parse(PublicKey);
            var PvtKey = SignPrivateKey.Parse(PrivateKey);
            return new SignKeyPair(PvtKey, PubKey);
        }

        /// <summary>
        /// HTTP Endpoint of the node.
        /// </summary>
        [JsonProperty("http_endpoint")]
        public string HttpEndpoint { get; set; } = "0.0.0.0:5000";

        /// <summary>
        /// P2P Endpoint of the node.
        /// </summary>
        [JsonProperty("p2p_endpoint")]
        public string P2PEndpoint { get; set; } = "0.0.0.0:7000";

        /// <summary>
        /// P2P Seeds. (Initial Contacts)
        /// </summary>
        [JsonProperty("p2p_seeds")]
        public List<string> P2PSeeds { get; set; } = new List<string>();

        /// <summary>
        /// Genesis Setting file.
        /// </summary>
        [JsonIgnore]
        public string GenesisFile { get; set; }

        /// <summary>
        /// Block Directory to store the entire chain.
        /// </summary>
        [JsonProperty("block_dir")]
        public string BlockDir { get; set; } = Path.Combine(GetAppDir(), "data", "blocks");

        /// <summary>
        /// State Directory to store the chain state (i.e. cache, transaction queue...).
        /// </summary>
        [JsonProperty("state_dir")]
        public string StateDir { get; set; } = Path.Combine(GetAppDir(), "data", "state");

        /// <summary>
        /// Module DLL files to load.
        /// </summary>
        [JsonProperty("modules")]
        public List<string> Modules { get; set; } = new List<string>();

        /// <summary>
        /// Module Assemblies.
        /// </summary>
        [JsonIgnore]
        public List<Assembly> ModuleAssemblies { get; } = new List<Assembly>();

        /// <summary>
        /// Module Instances.
        /// </summary>
        [JsonIgnore]
        public List<NodeModule> ModuleInstances { get; } = new List<NodeModule>();

        /// <summary>
        /// Module Extras.
        /// </summary>
        [JsonIgnore]
        public Dictionary<object, object> ModuleExtras { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Gets the application directory.
        /// </summary>
        /// <returns></returns>
        private static string GetAppDir() => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Parse the arguments and build the node options instance.
        /// </summary>
        /// <param name="Arguments"></param>
        /// <param name="Delegate">Invoked after node itself arguments parsed excluding module options.</param>
        /// <returns></returns>
        public static NodeOptions Parse(string[] Arguments, Action<NodeOptions> Delegate = null)
        {
            var Options = new NodeOptions();
            var Queue = new Queue<string>(Arguments);
            var Remainder = new Queue<string>();

            string DequeueOnce(string Name)
            {
                if (!Queue.TryDequeue(out var Value) || string.IsNullOrWhiteSpace(Value))
                    throw new InvalidOperationException($"`{Name}` required but not specified.");

                return Value;
            }

            while (Queue.TryDequeue(out var Option))
            {
                if (!Option.StartsWith('-'))
                {
                    Remainder.Enqueue(Option);
                    continue;
                }

                switch (Option.TrimStart('-'))
                {
                    case "config":
                    case "c":
                        {
                            var FileName = DequeueOnce("File Name");
                            if (!File.Exists(FileName))
                                throw new FileNotFoundException($"the configuration file, {FileName} not found.");

                            Options = JsonConvert.DeserializeObject<NodeOptions>(File.ReadAllText(FileName));
                        }
                        break;

                    case "genesis":
                        {
                            var FileName = DequeueOnce("File Name");
                            if (!File.Exists(FileName))
                                throw new FileNotFoundException($"the `genesis.json` file, {FileName} not found.");

                            Options.GenesisFile = Path.GetFullPath(FileName);
                        }
                        break;

                    case "http:listen":
                        {
                            var EndpointStr = DequeueOnce("Http Endpoint");
                            if (!IPEndPoint.TryParse(EndpointStr, out var Endpoint))
                                throw new InvalidOperationException($"`{EndpointStr}` is not valid endpoint address.");

                            Options.HttpEndpoint = Endpoint.ToString();
                        }
                        break;

                    case "p2p:listen":
                        {
                            var EndpointStr = DequeueOnce("P2P Endpoint");
                            if (!IPEndPoint.TryParse(EndpointStr, out var Endpoint))
                                throw new InvalidOperationException($"`{EndpointStr}` is not valid endpoint address.");

                            Options.P2PEndpoint = Endpoint.ToString();
                        }
                        break;

                    case "p2p:peer":
                        {
                            var EndpointStr = DequeueOnce("P2P Peer Endpoint");
                            if (!IPEndPoint.TryParse(EndpointStr, out var Endpoint))
                                throw new InvalidOperationException($"`{EndpointStr}` is not valid endpoint address.");

                            Options.P2PSeeds.Add(Endpoint.ToString());
                        }
                        break;

                    case "block:dir":
                        {
                            var Dir = DequeueOnce("Block Directory");
                            try { Options.BlockDir = Path.GetFullPath(Dir); }
                            catch
                            {
                                throw new InvalidOperationException($"`{Dir}` is not valid directory path name.");
                            }
                        }
                        break;

                    case "state:dir":
                        {
                            var Dir = DequeueOnce("State Directory");
                            try { Options.StateDir = Path.GetFullPath(Dir); }
                            catch
                            {
                                throw new InvalidOperationException($"`{Dir}` is not valid directory path name.");
                            }
                        }
                        break;

                    case "chain:actor":
                        {
                            var Actor = DequeueOnce("Actor Name");
                            if (!GActor.CanUse(Actor))
                                throw new InvalidOperationException($"`{Actor}` contains one or more unusable charactors.");

                            Options.Actor = Actor;
                        }
                        break;

                    case "chain:pvt":
                        {
                            var PvtStr = DequeueOnce("Private Key");
                            if (!SignPrivateKey.TryParse(PvtStr, out var Pvt))
                                throw new InvalidOperationException($"`{PvtStr}` is not valid private key string.");

                            Options.PrivateKey = Pvt.ToString();
                        }
                        break;

                    case "chain:pub":
                        {
                            var PubStr = DequeueOnce("Public Key");
                            if (!SignPublicKey.TryParse(PubStr, out var Pub))
                                throw new InvalidOperationException($"`{PubStr}` is not valid public key string.");

                            Options.PublicKey = Pub.ToString();
                        }
                        break;

                    case "module":
                    case "m":
                        {
                            var ModuleFile = DequeueOnce("Module DLL");
                            if (!File.Exists(ModuleFile))
                                throw new InvalidOperationException($"`{ModuleFile}` not found.");

                            try
                            {
                                Options.ModuleAssemblies.Add(Assembly.LoadFrom(ModuleFile));
                            }
                            catch
                            {
                                throw new InvalidOperationException($"`{ModuleFile}` is not valid module DLL.");
                            }
                        }
                        break;

                    default:
                        Remainder.Enqueue(Option);
                        break;
                }
            }

            Delegate?.Invoke(Options);
            LoadModuleAssemblies(Options);
            InstantiateModuleInstances(Options);

            /* And finally, capture all module options from the remainders. */
            foreach (var Each in Options.ModuleInstances)
            {
                ModelHelpers.Swap(ref Remainder, ref Queue);
                Each.CaptureOptions(Queue, Remainder, Options);
            }

            if (Remainder.Count > 0)
            {
                throw new InvalidOperationException($"`{Remainder.Peek()}` is unknown option.");
            }

            return Options;
        }

        /// <summary>
        /// Instantiate all module instances.
        /// </summary>
        /// <param name="Options"></param>
        private static void InstantiateModuleInstances(NodeOptions Options)
        {
            var ModuleTypes = new List<Type>();
            var ModuleInstances = new List<NodeModule>();
            var CompletedModuleTypes = new HashSet<Type>();

            foreach (var EachAssembly in Options.ModuleAssemblies.Distinct())
            {
                ModuleTypes.AddRange(EachAssembly
                    .GetTypes().Where(X => !X.IsAbstract)
                    .Where(X => X.IsAssignableTo(typeof(NodeModule))));
            }

            while (ModuleTypes.Count > 0)
            {
                var CurrentModuleTypes = ModuleTypes.Distinct()
                    .Where(X => !CompletedModuleTypes.Contains(X))
                    .ToArray();

                ModuleTypes.Clear();

                foreach (var EachModuleType in CurrentModuleTypes)
                {
                    var Ctor = EachModuleType.GetConstructor(Type.EmptyTypes);
                    if (Ctor is null)
                    {
                        throw new InvalidOperationException($"`{EachModuleType.FullName}` has no default constructor.");
                    }

                    var Module = Ctor.Invoke(EMPTY_ARGS) as NodeModule;

                    if (Module.Dependencies is not null && Module.Dependencies.Length > 0)
                        ModuleTypes.AddRange(Module.Dependencies);

                    ModuleInstances.Add(Module);
                    CompletedModuleTypes.Add(EachModuleType);
                }
            }

            OrganizeModuleInstances(Options, ModuleInstances);
        }

        /// <summary>
        /// Organize all module instances.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="ModuleInstances"></param>
        private static void OrganizeModuleInstances(NodeOptions Options, List<NodeModule> ModuleInstances)
        {
            while (ModuleInstances.Count > 0)
            {
                var Independents = ModuleInstances
                    .Where(X => IsMetDependencies(X, Options)).ToList();

                if (Independents.Count <= 0)
                {
                    var Modules = ModuleInstances.Select(X => X.GetType().FullName);
                    throw new InvalidOperationException($"No dependency resolved: {string.Join(", ", Modules)}.");
                }

                ModuleInstances.RemoveAll(X => Independents.Contains(X));
                Independents.Sort((A, B) => B.Priority - A.Priority);

                Options.ModuleInstances.AddRange(Independents);
            }
        }

        /// <summary>
        /// Test whether the Node Module mets the prerequisites or not.
        /// </summary>
        /// <param name="Module"></param>
        /// <param name="Options"></param>
        /// <returns></returns>
        private static bool IsMetDependencies(NodeModule Module, NodeOptions Options)
        {
            if (Module.Dependencies is null || Module.Dependencies.Length <= 0)
                return true;

            var CompletedInstances = Options.ModuleInstances;
            var Counter = Module.Dependencies.Count(X => CompletedInstances.FindIndex(Y => Y.GetType().IsAssignableTo(X)) >= 0);

            if (Counter >= Module.Dependencies.Length)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Load Module Assemblies.
        /// </summary>
        /// <param name="Options"></param>
        private static void LoadModuleAssemblies(NodeOptions Options)
        {
            foreach (var EachModule in Options.Modules)
            {
                if (!File.Exists(EachModule))
                    throw new InvalidOperationException($"`{EachModule}` not found.");

                try
                {
                    Options.ModuleAssemblies.Add(Assembly.LoadFrom(EachModule));
                }
                catch
                {
                    throw new InvalidOperationException($"`{EachModule}` is not valid module DLL.");
                }
            }
        }
    }
}
