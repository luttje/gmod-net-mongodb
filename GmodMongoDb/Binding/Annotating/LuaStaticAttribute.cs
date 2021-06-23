using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.Annotating
{
    /// <summary>
    /// Apply this attribute to methods to make them available statically in Lua (without the need for an object instance)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class LuaStaticAttribute : LuaMethodAttribute
    {
        /// <summary>
        /// Whether the method marked with this attribute is the object initializer.
        /// This means the static functions table will get a __call metamethod that will call this initializer.
        /// </summary>
        /// <example>
        /// That allows you to do this in Lua:
        /// <code language="Lua"><![CDATA[
        /// local exampleObject = Example()
        /// ]]></code>
        /// </example>
        public bool IsInitializer { get; set; }

        /// <summary>
        /// When applying the attribute to a method you can specify the static method's name
        /// </summary>
        /// <param name="name">The static method's name</param>
        public LuaStaticAttribute(string name = null)
            : base(name) { }
    }
}
