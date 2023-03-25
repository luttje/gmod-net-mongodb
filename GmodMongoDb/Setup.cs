using GmodMongoDb.Binding;
using GmodNET.API;
using System;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb
{
    /// <inheritdoc/>
    public class Setup : IModule
    {
        /// <inheritdoc/>
        public string ModuleName => "GmodMongoDb";

        /// <inheritdoc/>
        public string ModuleVersion => "0.9";

        private DynamicWrapper wrapper = null;

        /// <inheritdoc/>
        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            // Two random types we only use to get relevant assemblies.
            var mongoDbAssemblies = new[] {
                typeof(MongoDB.Driver.MongoClient).Assembly,
                typeof(MongoDB.Bson.BsonDocument).Assembly,
            };

            wrapper = new DynamicWrapper(lua, "MongoDB");

            foreach (var assembly in mongoDbAssemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null);

                foreach (var type in types)
                {
                    wrapper.RegisterType(type);
                }
            }

            lua.Print("GmodMongoDb loaded!");
            lua.Print(lua.GetStack());
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            wrapper.Dispose();

            lua.Print("GmodMongoDb unloaded!");
            lua.Print(lua.GetStack());
        }
    }
}
