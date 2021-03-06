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
    /// <![CDATA[
    /// [ConvertsNetType(typeof(ExampleClass))]
    /// ]]>
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
        /// Pulls or reads an object from the Lua stack at the given position and tries to convert it to a .NET object.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="value">The value pulled from the Lua stack</param>
        /// <param name="stackPos">The position to pull/read the object from</param>
        /// <param name="forceKeepOnStack">Keep the object on the stack, false to remove it</param>
        /// <returns>Whether the value was succesfully parsed</returns>
        public abstract bool TryParse(ILua lua, out TTarget value, int stackPos = -1, bool forceKeepOnStack = false);
    }

    /// <summary>
    /// Adds extensions relating to data transformation.
    /// </summary>
    public static class LuaTransformerExtensions
    {
        /// <summary>
        /// Instantiates a transformer of the given type and serves it the value to convert to Lua.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="transformer">The type of the transformer class (must inherit <see cref="LuaValueTransformer{TInput}"/>)</param>
        /// <param name="value">The value to have the transformer convert to Lua</param>
        /// <returns>The amount of Lua objects pushed to the stack by the transformer</returns>
        /// <exception cref="ArgumentException">Thrown when the transformer type given does not inherit <see cref="LuaValueTransformer{TInput}"/></exception>
        public static int ApplyTransformerConvert(this ILua lua, Type transformer, object value)
        {
            var converter = Activator.CreateInstance(transformer);

            if (converter is not BaseLuaValueTransformer)
                throw new ArgumentException("Invalid converter provided to ApplyTransformerConvert! Did you forget to inherit LuaValueTransformer<Type>?");

            return (int)transformer.GetMethod("Convert").Invoke(converter, new object[]
            {
                lua,
                value
            });
        }

        /// <summary>
        /// Instantiates a transformer of the given type and executes it's TryParse to parse a value at the given Lua stack position.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="transformer">The type of the transformer class (must inherit <see cref="LuaValueTransformer{TInput}"/>)</param>
        /// <param name="value">The value pulled from the Lua stack</param>
        /// <param name="stackPos">The position to pull/read the object from</param>
        /// <param name="forceKeepOnStack">Keep the object on the stack, false to remove it</param>
        /// <returns>Whether the value was succesfully parsed as returned by the transformer's <see cref="LuaValueTransformer{TInput}.TryParse"/> method.</returns>
        /// <exception cref="ArgumentException">Thrown when the transformer type given does not inherit <see cref="LuaValueTransformer{TInput}"/></exception>
        public static bool ApplyTransformerParse(this ILua lua, Type transformer, out object value, int stackPos = -1, bool forceKeepOnStack = false)
        {
            var converter = Activator.CreateInstance(transformer);

            if (converter is not BaseLuaValueTransformer)
                throw new ArgumentException("Invalid converter provided to ApplyTransformerConvert! Did you forget to inherit LuaValueTransformer<Type>?");

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
