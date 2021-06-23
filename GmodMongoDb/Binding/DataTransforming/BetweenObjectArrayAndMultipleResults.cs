using GmodMongoDb.Binding;
using GmodNET.API;
using System;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Transformers from an object array to multiple Lua return types or vice versa.
    /// </summary>
    /// <example>
    /// Consider this in C#: 
    /// <code><![CDATA[
    /// return object[]{ 1, 2, 3, 4 }
    /// ]]></code>
    /// Would equal this in Lua after the transformer is used: 
    /// <code language="Lua"><![CDATA[
    /// return 1, 2, 3, 4
    /// ]]></code>
    /// </example>
    public sealed class BetweenObjectArrayAndMultipleResults : LuaValueTransformer<object[]>
    {
        ///<inheritdoc/>
        public override int Convert(ILua lua, object[] results)
        {
            int stack = 0;

            for (int i = 0; i < results.Length; i++)
            {
                var item = results[i];
                stack += TypeTools.PushType(lua, item?.GetType(), item);
            }

            return stack;
        }

        ///<inheritdoc/>
        public override bool TryParse(ILua lua, out object[] value, int stackPos = -1, bool forceKeepOnStack = false)
        {
            throw new NotImplementedException();
        }
    }
}
