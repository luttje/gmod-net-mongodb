using GmodMongoDb.Binding;
using GmodNET.API;
using System;

namespace GmodMongoDb
{
    /// <inheritdoc/>
    public class Setup : IModule
    {
        /// <summary>
        /// Name used for prefixing global constants in Lua.
        /// </summary>
        public const string CONSTANT_PREFIX = "GMOD_MONGODB_";
        
        /// <inheritdoc/>
        public string ModuleName => "GmodMongoDb";

        /// <inheritdoc/>
        public string ModuleVersion => "0.9.1";

        /// <summary>
        /// Reference to the wrapper that helps with binding C# methods to Lua.
        /// </summary>
        private DynamicWrapper wrapper = null;

        /// <inheritdoc/>
        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            wrapper = new DynamicWrapper(lua, "MongoDB");

            // We just use the types to get relevant assemblies. Those will be used to reach all types that need to be bound.
            wrapper.RegisterTypes(new[] {
                typeof(MongoDB.Driver.MongoClient).Assembly,
                typeof(MongoDB.Bson.BsonDocument).Assembly,
                typeof(MongoDB.Driver.Core.Operations.AsyncCursor<>).Assembly,
            });

            Console.WriteLine("[GmodMongoDb] loaded.");
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            wrapper.Dispose();
            wrapper = null;

            Console.WriteLine("[GmodMongoDb] unloaded.");
        }
    }
}
