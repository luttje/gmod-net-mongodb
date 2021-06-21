using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmodMongoDb.Binding
{
    public static class LuaExtensions
    {
        /// <summary>
        /// Prints a message the next Lua tick
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="message"></param>
        public static void PrintFromAsync(this ILua lua, string message)
        {
            LuaTaskScheduler.AddTask(() => lua.Print(message));
        }

        public static void Print(this ILua lua, string message)
        {
            lua.PushSpecial(GmodNET.API.SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "print");
            lua.PushString(message);
            lua.MCall(1, 0);
            lua.Pop(1);
        }

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
