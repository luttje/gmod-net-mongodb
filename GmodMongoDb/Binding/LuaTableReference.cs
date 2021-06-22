using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    public class LuaTableReference : LuaReference
    {
        /// <summary>
        /// Create a reference for the table currently on the given position of the stack (or the top by default)
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos">The stack position of the function to reference</param>
        public LuaTableReference(ILua lua, int stackPos = -1, bool forceKeepOnStack = false)
            : base(lua, stackPos, forceKeepOnStack)
        {

        }

        ///<inheritdoc/>
        protected override bool IsValid(int stackPos)
        {
            TYPES type = (TYPES)lua.GetType(stackPos);

            if (type != TYPES.TABLE)
            {
                throw new ArgumentException($"Invalid type ({lua.GetTypeName(type)}) detected! Should be a table!");
            }

            return base.IsValid(stackPos);
        }

        /// <summary>
        /// Iterate the Lua table through a callback
        /// </summary>
        /// <param name="action">A callback which is given the key and value of each item in the table</param>
        public void ForEach(Action<object, object> action)
        {
            this.Push();
            for (lua.PushNil(); lua.Next(-2) != 0; lua.Pop(1))
            {
                object key = TypeConverter.PullType(lua, -2, true);
                object value = TypeConverter.PullType(lua, -1, true);

                action(key, value);
            }
            lua.Pop(1); // Pops the table
        }
    }
}
