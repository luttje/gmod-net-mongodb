using System;

namespace GmodMongoDb.Binding.Annotating
{
    public class LuaBindingAttribute : Attribute
    {
        /// <summary>
        /// An binding connected to this type
        /// </summary>
        public LuaBinding BoundType { get; internal set; }
    }
}