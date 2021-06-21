using System;

namespace GmodMongoDb.Binding.Annotating
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LuaMetaTableAttribute : Attribute
    {
        public string Name { get; set; }

        public LuaMetaTableAttribute(string name = null)
        {
            this.Name = name;
        }
    }
}
