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

        /// <inheritdoc/>
        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.CreateTable(); // MongoDB table

            // Two random types we only use to get relevant assemblies.
            var mongoDbAssemblies = new[] {
                typeof(MongoDB.Driver.MongoClient).Assembly,
                typeof(MongoDB.Bson.BsonDocument).Assembly,
            };

            lua.SetField(-2, "MongoDB"); // MongoDB table
            lua.Pop(); // Pop the Global table

            // The same as above, but using the InteropRegister
            foreach (var assembly in mongoDbAssemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null);

                foreach (var type in types)
                {
                    DynamicWrapper.RegisterType(lua, type, "MongoDB");
                }
            }

            LuaExtensions.Print(lua, "GmodMongoDb loaded!");
            LuaExtensions.Print(lua, lua.GetStack());
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            // Set MongoDB to nil to release all references to the types.
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, "MongoDB");
            lua.Pop();

            LuaExtensions.Print(lua, "GmodMongoDb unloaded!");
            LuaExtensions.Print(lua, lua.GetStack());
        }
    }
}
