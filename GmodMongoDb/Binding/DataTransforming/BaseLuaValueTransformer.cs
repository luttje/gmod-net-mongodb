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
    /// <typeparam name="TInput">The type to and from which this transformer converts</typeparam>
    public abstract class BaseLuaValueTransformer<TInput>
    {
        /// <summary>
        /// Since a transformer is instantiated with Activator.CreateInstance there should be no parameters on the constructor.
        /// </summary>
        public BaseLuaValueTransformer() { }

        /// <summary>
        /// Converts a given .NET object and pushes it on the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="input">The .NET object to convert to Lua</param>
        /// <returns>The amount of objects pushed to the Lua stack</returns>
        public abstract int Convert(ILua lua, TInput input);

        /// <summary>
        /// Pulls an object from the Lua stack and converts it to a .NET object.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns>The parsed .NET object</returns>
        public abstract TInput Parse(ILua lua);
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
        /// <param name="transformer">The type of the transformer class (must inherit <see cref="BaseLuaValueTransformer{TInput}"/>)</param>
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
    }
}
