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
            TypeTools.DiscoverDataTransformers();
            TypeTools.CreateDiscoveredMetaTableDefinitions(lua);
        }

        /// <inheritdoc/>
        public void Unload(ILua lua)
        {
            TypeTools.CleanUpStaticFunctionTables(lua);
            LuaTaskScheduler.UnregisterLuaCallback(lua);
            ReferenceManager.KillAll();
        }
    }
}
