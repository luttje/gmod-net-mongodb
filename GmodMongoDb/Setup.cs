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

            // The same as above, but using the InteropRegister
            foreach (var assembly in mongoDbAssemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null);

                foreach (var type in types)
                {
                    wrapper.RegisterType(type);
                }
            }

            LuaExtensions.Print(lua, "GmodMongoDb loaded!");
            LuaExtensions.Print(lua, lua.GetStack());
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            wrapper.Dispose();

            LuaExtensions.Print(lua, "GmodMongoDb unloaded!");
            LuaExtensions.Print(lua, lua.GetStack());
        }
    }
}
