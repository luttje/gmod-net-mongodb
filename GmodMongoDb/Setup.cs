using GmodMongoDb.Binding;
using GmodNET.API;
using System;

namespace GmodMongoDb
{
    /// <inheritdoc/>
    public class Setup : GmodNET.API.IModule
    {
        /// <inheritdoc/>
        public string ModuleName => "GmodMongoDb";

        /// <inheritdoc/>
        public string ModuleVersion => "0.9";

        /// <inheritdoc/>
        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            LuaTaskScheduler.RegisterLuaCallback(lua);
            TypeConverter.DiscoverDataTransformers();
            TypeConverter.CreateDiscoveredMetaTableDefinitions(lua);

            Mongo mongo = new(lua);

            TypeConverter.GenerateUserDataFromObject(lua, mongo);

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.Insert(-2);
            lua.SetField(-2, "mongo");
            lua.Pop(1); // pop the global table
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, "mongo");
            lua.Pop(1); // pop the global table

            LuaTaskScheduler.UnregisterLuaCallback(lua);
            ReferenceManager.KillAll();
        }
    }
}
