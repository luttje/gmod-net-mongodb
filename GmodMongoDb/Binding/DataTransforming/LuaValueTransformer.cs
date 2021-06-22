using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Transformers derive from this class. Transformers help convert between .NET and Lua types.
    /// </summary>
    /// <example>
    /// Your subclass MUST register which type it converts to and from by registering with the following attribute.
    /// <code>
    /// [ConvertsNetType(typeof(ExampleClass))]
    /// </code>
    /// </example>
    /// <typeparam name="TTarget">The type to return or accept in the methods</typeparam>
    public abstract class LuaValueTransformer<TTarget> : BaseLuaValueTransformer
    {
        /// <summary>
        /// Since a transformer is instantiated with Activator.CreateInstance there should be no parameters on the constructor.
        /// </summary>
        public LuaValueTransformer() { }

        /// <summary>
        /// Converts a given .NET object and pushes it on the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="input">The .NET object to convert to Lua</param>
        /// <returns>The amount of objects pushed to the Lua stack</returns>
        public abstract int Convert(ILua lua, TTarget input);

        /// <summary>
        /// Pulls or reads an object from the Lua stack and tries to convert it to a .NET object.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="value">The value pulled from the Lua stack</param>
        /// <param name="forceKeepOnStack">Do not pop/remove the object</param>
        /// <returns></returns>
        public abstract bool TryParse(ILua lua, out TTarget value, int stackPos = -1, bool forceKeepOnStack = false);
    }

    /// <summary>
    /// Adds extensions relating to data transformation.
    /// </summary>
    public static class LuaTransformerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="transformer">The type of the transformer class (must inherit <see cref="LuaValueTransformer{TInput}"/>)</param>
        /// <param name="value">The value to apply the transformer to</param>
        /// <returns>The amount of Lua objects pushed to the stack by the transformer</returns>
        public static int ApplyTransformerConvert(this ILua lua, Type transformer, object value)
        {
            var converter = Activator.CreateInstance(transformer);

            return (int)transformer.GetMethod("Convert").Invoke(converter, new object[]
            {
                lua,
                value
            });
        }

        public static bool ApplyTransformerParse(this ILua lua, Type transformer, out object value, int stackPos = -1, bool forceKeepOnStack = false)
        {
            var converter = Activator.CreateInstance(transformer);
            var parameters = new object[]
            {
                lua,
                null, // 1
                stackPos,
                forceKeepOnStack,
            };
            var success = (bool)transformer.GetMethod("TryParse").Invoke(converter, parameters);

            if (success)
                value = parameters[1];
            else
                value = null;

            return success;
        }
    }
}
