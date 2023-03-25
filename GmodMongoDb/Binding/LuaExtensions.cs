using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helpful functions to debug or message in Lua
    /// </summary>
    public static class LuaExtensions
    {
        /// <summary>
        /// Prints a message in Lua
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="message">The message to show</param>
        public static void Print(this ILua lua, string message)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "print");
            lua.PushString(message);
            lua.MCall(1, 0);
            lua.Pop(1);
        }

        /// <summary>
        /// Builds a string representation of the stack by traversing all values on the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns>A string containing the types on the stack</returns>
        public static string GetStack(this ILua lua)
        {
            int top = lua.Top();
            string stack = $"Stack (count={top}):\n";

            for (int i = 1; i <= top; i++)
            {
                int type = lua.GetType(i);

                stack += $"{i}\t{lua.GetTypeName(type)}\t";

                stack += (TYPES)type switch
                {
                    TYPES.NUMBER => $"NUMBER: {lua.GetNumber(i)}\n",
                    TYPES.STRING => $"STRING: {lua.GetString(i)}\n",
                    TYPES.BOOL => $"BOOLEAN: {(lua.GetBool(i) ? "true" : "false")}\n",
                    TYPES.NIL => "NIL\n",
                    _ => "POINTER\n",
                };
            }

            return $"{stack}\n";
        }
    }
}
