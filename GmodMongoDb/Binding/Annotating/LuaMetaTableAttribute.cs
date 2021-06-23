using System;

namespace GmodMongoDb.Binding.Annotating
{
    /// <summary>
    /// Apply this attribute to classes to change the metatable type name they'll receive in Lua
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LuaMetaTableAttribute : Attribute
    {
        /// <summary>
        /// The desired metatable type name in Lua
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// When applying the attribute to a class you can specify a metatable type name
        /// </summary>
        /// <param name="name">The metatable type name</param>
        public LuaMetaTableAttribute(string name = null)
        {
            this.Name = name;
        }
    }
}
