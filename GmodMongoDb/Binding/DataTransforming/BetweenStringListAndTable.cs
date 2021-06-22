using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Converts between a string list and a Lua table containing those strings.
    /// </summary>
    public sealed class BetweenStringListAndTable : LuaValueTransformer<List<string>>
    {
        ///<inheritdoc/>
        public override int Convert(ILua lua, List<string> results)
        {
            lua.CreateTable();
            int i = 1;
            foreach (string item in results)
            {
                lua.PushString(item);
                lua.PushNumber(i++);
                lua.Insert(-2);
                lua.RawSet(-3);
            }

            return 1;
        }

        ///<inheritdoc/>
        public override bool TryParse(ILua lua, out List<string> value, int stackPos = -1, bool forceKeepOnStack = false)
        {
            throw new NotImplementedException();
        }
    }
}
