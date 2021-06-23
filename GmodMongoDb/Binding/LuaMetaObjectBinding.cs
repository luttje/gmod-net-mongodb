using GmodMongoDb.Binding.Annotating;
using GmodNET.API;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// The baseclass from which metatable classes can inherit. These classes specify how the metatable should look in Lua.
    /// Metatable classes can be marked with [LuaMetaTable] to give them an explicit type name. Otherwise their class name will be used.
    /// </summary>
    public abstract class LuaMetaObjectBinding
    {
        /// <summary>
        /// The metatable type id for this object, as returned by `lua.CreateMetaTable`
        /// </summary>
        public int? MetaTableTypeId { get; set; }

        /// <summary>
        /// The pointer to the Lua instance of this object. Only filled when a method on this class is called from Lua.
        /// </summary>
        public int? Reference
        {
            get => reference; 
            
            internal set
            {
                if (value == null && reference != null)
                    lua.ReferenceFree((int)reference);

                reference = value;
            }
        }
        private int? reference;

        /// <summary>
        /// The Lua environment where this object lives
        /// </summary>
        protected ILua lua;

        /// <summary>
        /// Instantiates the .NET Object, informing it of the Lua environment it's part of.
        /// </summary>
        /// <param name="lua">The Lua environment where this object lives</param>
        public LuaMetaObjectBinding(ILua lua)
        {
            this.MetaTableTypeId = null;
            this.Reference = null;
            this.lua = lua;
        }
    }
}
