using GmodNET.API;
using System;
using System.Collections.Generic;
using static MongoDB.Driver.WriteConcern;
using System.ComponentModel;
using System.Diagnostics;
using SharpCompress.Common;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Map of Lua types to .NET types
    /// </summary>
    internal class LuaTable
    {
        private List<LuaTableElement> luaTableElements { get; set; }

        /// <summary>
        /// Reads the Lua table at the top of the stack and returns it as a <see cref="LuaTable"/>.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static LuaTable Get(ILua lua, int stackPos)
        {
            // Iterate over the table and read all key-value pairs
            return null;
        }

        /// <summary>
        /// Pushes this Lua table to the top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        internal void Push(ILua lua)
        {
        }

        internal struct LuaTableElement
        {
            public object Key { get; private set; }

            /// <summary>
            /// Value of any type, can also be a LuaTableElement
            /// </summary>
            public object Value { get; private set; }

            public LuaTableElement(object key, object value)
            {
                Key = key;
                Value = value;
            }

            public override string ToString()
            {
                return $"{Key} = {Value}";
            }
        }
    }
}