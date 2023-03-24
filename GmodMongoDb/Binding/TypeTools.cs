using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helps converting between .NET objects/types and Lua types
    /// </summary>
    public static class TypeTools
    {
        private static readonly Dictionary<int, Type> MetaTableTypeIds = new();

        /// <summary>
        /// Push a value of a specific type to the Lua stack. Applies a transformer where possible.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type">The type of the value to push</param>
        /// <param name="value">The value to push</param>
        public static int PushType(ILua lua, Type type, object value)
        {
            if (type == null || value == null)
            {
                lua.PushNil();

                return 1;
            }
            else if (type == typeof(string))
            {
                lua.PushString((string)value);

                return 1;
            }
            else if (type == typeof(bool))
            {
                lua.PushBool((bool)value);

                return 1;
            }
            else if (type == typeof(int)
                || type == typeof(float)
                || type == typeof(double))
            {
                lua.PushNumber(Convert.ToDouble(value));

                return 1;
            }
            else if (type == typeof(IntPtr))
            {
                lua.ReferencePush((int)value);

                return 1;
            }
            else
                // TODO
                throw new NotImplementedException($"This type is not registered for conversion to Lua from .NET! Consider building a Transformer. Type is: {type.FullName}");
        }

        /// <summary>
        /// Pop a value from the Lua stack and convert it to the specified .NET type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type">The expected type of the value on the stack</param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeepOnStack">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static object PullType(ILua lua, Type type, int stackPos = -1, bool forceKeepOnStack = false)
        {
            bool pop = true;
            object value;

            if (type == typeof(string))
                value = lua.GetString(stackPos);
            else if (type == typeof(bool))
                value = lua.GetBool(stackPos);
            else if (type == typeof(int)
                || type == typeof(float)
                || type == typeof(double))
                value = lua.GetNumber(stackPos);
            else
                // TODO
                throw new NotImplementedException($"This type is not registered for conversion from Lua to .NET! Consider building a Transformer. Type is: {type.FullName}");

            if (pop && !forceKeepOnStack)
                lua.Remove(stackPos);

            return value;
        }

        /// <summary>
        /// Pop a value from the Lua stack and try convert it to an associated .NET type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeepOnStack">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static object PullType(ILua lua, int stackPos = -1, bool forceKeepOnStack = false)
        {
            TYPES luaType = (TYPES)lua.GetType(stackPos);
            Type type = LuaTypeToDotNetType(luaType);

            return PullType(lua, type, stackPos, forceKeepOnStack);
        }

        /// <summary>
        /// Pop a value from the Lua stack and convert it to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value on the stack</typeparam>
        /// <param name="lua"></param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeepOnStack">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static T PullType<T>(ILua lua, int stackPos = -1, bool forceKeepOnStack = false)
        {
            return (T) PullType(lua, typeof(T), stackPos, forceKeepOnStack);
        }

        /// <summary>
        /// Try to convert a Lua type to a metatable.
        /// </summary>
        /// <param name="luaType">The type id that's suspected to be a metatable type</param>
        /// <param name="result">The converted .NET type on success. Null otherwise.</param>
        /// <returns>If the type is succesfully converted to a metatable type.</returns>
        public static bool TryGetMetaTableType(int luaType, out Type result)
        {
            if (MetaTableTypeIds.ContainsKey((int)luaType))
            {
                result = MetaTableTypeIds[(int)luaType];
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Convert a specified Lua type to a .NET type.
        /// </summary>
        /// <param name="luaType">The Lua type to convert</param>
        /// <returns>The converted .NET type</returns>
        public static Type LuaTypeToDotNetType(TYPES luaType)
        {
            if (TryGetMetaTableType((int) luaType, out Type result))
                return result;

            return luaType switch
            {
                TYPES.NUMBER => typeof(double),
                TYPES.STRING => typeof(string),
                TYPES.BOOL => typeof(bool),
                TYPES.NIL => null,
                _ => throw new NotImplementedException($"This type is not registered for conversion from Lua to .NET! Type is: {luaType}"),
            };
        }

        /// <summary>
        /// Convert a specified Lua type to a .NET type.
        /// </summary>
        /// <param name="luaType">The Lua type to convert (must be castable to <see cref="GmodNET.API.TYPES"/>)</param>
        /// <returns>The converted .NET type</returns>
        public static Type LuaTypeToDotNetType(int luaType)
            => LuaTypeToDotNetType((TYPES)luaType);
    }
}
