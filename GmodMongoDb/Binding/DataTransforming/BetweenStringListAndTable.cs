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
    public class BetweenStringListAndTable : BaseLuaValueTransformer<List<string>>
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
        public override List<string> Parse(ILua lua)
        {
            throw new NotImplementedException();
        }
    }
}
