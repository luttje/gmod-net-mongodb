using GmodMongoDb.Binding;
using GmodNET.API;
using System;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Converts between an object array and multiple Lua results.
    /// </summary>
    public class BetweenObjectArrayAndMultipleResults : BaseLuaValueTransformer<object[]>
    {
        ///<inheritdoc/>
        public override int Convert(ILua lua, object[] results)
        {
            int stack = 0;

            for (int i = 0; i < results.Length; i++)
            {
                var item = results[i];
                stack += BindingHelper.PushType(lua, item?.GetType(), item);
            }

            return stack;
        }

        ///<inheritdoc/>
        public override object[] Parse(ILua lua)
        {
            throw new NotImplementedException();
        }
    }
}
