using GmodMongoDb.Binding;
using GmodNET.API;
using System;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Converts between an object array and multiple Lua results.
    /// </summary>
    public sealed class BetweenObjectArrayAndMultipleResults : LuaValueTransformer<object[]>
    {
        ///<inheritdoc/>
        public override int Convert(ILua lua, object[] results)
        {
            int stack = 0;

            for (int i = 0; i < results.Length; i++)
            {
                var item = results[i];
                stack += TypeConverter.PushType(lua, item?.GetType(), item);
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
