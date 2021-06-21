using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.Annotating
{
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class LuaValueTransformerAttribute : Attribute
    {
        public Type Transformer { get; set; }

        public LuaValueTransformerAttribute(Type transformer)
        {
            this.Transformer = transformer;
        }
    }
}
