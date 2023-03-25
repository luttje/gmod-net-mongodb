﻿using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helps converting between .NET objects/types and Lua types
    /// </summary>
    public static class TypeTools
    {
        /// <summary>
        /// Returns whether the given type is a primitive type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsLuaType(Type type)
        {
            return type == null
                || type == typeof(string)
                || type == typeof(bool)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(IntPtr);
        }

        /// <summary>
        /// Push a value of a specific type to the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type">The type of the value to push</param>
        /// <param name="value">The value to push</param>
        public static void PushType(ILua lua, Type type, object value)
        {
            if (type == null || value == null)
            {
                lua.PushNil();
            }
            else if (type == typeof(string))
            {
                lua.PushString((string)value);
            }
            else if (type == typeof(bool))
            {
                lua.PushBool((bool)value);
            }
            else if (type == typeof(int)
                || type == typeof(float)
                || type == typeof(double))
            {
                lua.PushNumber(Convert.ToDouble(value));
            }
            else if (type == typeof(LuaTable))
                ((LuaTable)value).Push(lua);
            else if (type == typeof(IntPtr))
            {
                lua.ReferencePush((int)value);
            }
            else
            {
                throw new ArgumentException("Unsupported type: " + type.FullName);
            }
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
            else if (type == typeof(LuaTable))
                value = LuaTable.Get(lua, stackPos);
            else if (type == typeof(IntPtr))
            {
                value = (IntPtr) lua.ReferenceCreate();
                pop = false;
            }
            else
            {
                throw new ArgumentException("Unsupported type: " + type.FullName);
            }

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
        /// Convert a specified Lua type to a .NET type.
        /// </summary>
        /// <param name="luaType">The Lua type to convert</param>
        /// <returns>The converted .NET type</returns>
        public static Type LuaTypeToDotNetType(TYPES luaType)
        {
            //if (TryGetMetaTableType((int) luaType, out Type result))
            //    return result;

            return luaType switch
            {
                TYPES.NUMBER => typeof(double),
                TYPES.STRING => typeof(string),
                TYPES.BOOL => typeof(bool),
                TYPES.TABLE => typeof(LuaTable),
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

        /// <summary>
        /// Converts the parameters to the types specified in the <paramref name="parameterInfos"/> array.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="parameterInfos"></param>
        /// <returns></returns>
        public static object[] NormalizeParameters(object[] parameters, ParameterInfo[] parameterInfos)
        {
            object[] normalizedParameters = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                if (i < parameters.Length)
                {
                    // Cast the parameter to the expected type
                    var expectedType = parameterInfos[i].ParameterType;

                    if (expectedType.IsEnum)
                    {
                        // If the expected type is an enum, try to convert the parameter to the underlying type of the enum
                        normalizedParameters[i] = Enum.ToObject(expectedType, parameters[i]);
                    }
                    else
                    {
                        // If the expected type is not an enum, try to convert the parameter to the expected type
                        normalizedParameters[i] = Convert.ChangeType(parameters[i], expectedType);
                    }
                }
                else
                {
                    normalizedParameters[i] = parameterInfos[i].DefaultValue;
                }
            }

            return normalizedParameters;
        }
    }
}
