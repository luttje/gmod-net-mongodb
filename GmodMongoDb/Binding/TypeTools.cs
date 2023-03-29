using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helps converting between simple .NET and Lua types
    /// </summary>
    public static class TypeTools
    {
        /// <summary>
        /// Whether the given type is a numeric type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long) ||
                   type == typeof(ulong) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }
        
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
                || type == typeof(IntPtr)
                || IsNumericType(type);
        }

        /// <summary>
        /// Returns whether the given object is a primitive type.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool IsLuaType(object instance)
        {
            return IsLuaType(instance?.GetType());
        }

        /// <summary>
        /// Push a value of to the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="value">The value to push</param>
        public static void PushType(ILua lua, object value)
        {
            PushType(lua, value.GetType(), value);
        }

        /// <summary>
        /// Push multiple values to the Lua stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="values">The value to push</param>
        public static void PushTypes(ILua lua, object[] values)
        {
            foreach (object value in values)
            {
                PushType(lua, value);
            }
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
            else if (IsNumericType(type))
            {
                lua.PushNumber(Convert.ToDouble(value));
            }
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

            if (type == null)
            {
                value = null;
            }
            else if (type == typeof(string))
                value = lua.GetString(stackPos);
            else if (type == typeof(bool))
                value = lua.GetBool(stackPos);
            else if (IsNumericType(type))
                value = lua.GetNumber(stackPos);
            else if (type == typeof(LuaFunction))
                value = LuaFunction.Get(lua, stackPos);
            else if (type == typeof(IntPtr))
            {
                value = (IntPtr)lua.ReferenceCreate();
                pop = false;
            }
            else
            {
                Console.WriteLine($"Unsupported type: {type.FullName}\r\n" + lua.GetStack());
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
                TYPES.FUNCTION => typeof(LuaFunction),
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

                    if (parameters[i] is LuaFunction luaFunction && LuaFunction.GetCastsTo(expectedType))
                    {
                        normalizedParameters[i] = luaFunction.CastTo(expectedType);
                    }
                    else if (expectedType.IsEnum)
                    {
                        normalizedParameters[i] = Enum.ToObject(expectedType, parameters[i]);
                    }
                    else
                    {
                        try
                        {
                            normalizedParameters[i] = Convert.ChangeType(parameters[i], expectedType);
                        }
                        catch (Exception)
                        {
                            normalizedParameters[i] = parameters[i];
                        }
                    }
                }
                else if (parameterInfos[i].HasDefaultValue)
                {
                    normalizedParameters[i] = parameterInfos[i].DefaultValue;
                }
                else
                {
                    normalizedParameters[i] = null;
                }
            }

            return normalizedParameters;
        }

        /// <summary>
        /// Converts the parameter types to the types specified in the <paramref name="parameterInfos"/> array.
        /// </summary>
        /// <param name="parameterTypes"></param>
        /// <param name="parameterInfos"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static List<object> NormalizeParameterTypes(Type[] parameterTypes, ParameterInfo[] parameterInfos)
        {
            var normalizedParameterTypes = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                if (i < parameterTypes.Length)
                {
                    // Cast the parameter to the expected type
                    var expectedType = parameterInfos[i].ParameterType;

                    if (parameterTypes[i] == typeof(LuaFunction) && LuaFunction.GetCastsTo(expectedType))
                    {
                        normalizedParameterTypes[i] = expectedType;
                    }
                    else
                    {
                        normalizedParameterTypes[i] = parameterTypes[i];
                    }
                }
                else if (parameterInfos[i].HasDefaultValue)
                {
                    normalizedParameterTypes[i] = Type.Missing;
                }
                else
                {
                    normalizedParameterTypes[i] = null;
                }
            }

            return new List<object>(normalizedParameterTypes);
        }

        /// <summary>
        /// Uses the provided parameter types to supplement the generic type arguments, until the desired number of generic type arguments is reached.
        /// </summary>
        /// <param name="genericTypeArgumentsAmount">Desired number of generic type arguments</param>
        /// <param name="genericTypeArgumentValues">Already provided type arguments</param>
        /// <param name="parameterTypes">Types of the parameters provided, used to suplement the generic type arguments.</param>
        /// <returns>The correct amount of types for the generic type arguments</returns>
        /// <exception cref="TargetInvocationException">Fails if not enough parameters were given to supplement the generic type arugments.</exception>
        internal static Type[] NormalizePossibleGenericTypeArguments(
            int genericTypeArgumentsAmount, 
            GenericType[] genericTypeArgumentValues, 
            List<Type> parameterTypes)
        {
            var normalizedGenericTypeValues = new GenericType?[genericTypeArgumentsAmount];
            var valueCount = genericTypeArgumentValues?.Length ?? 0;

            if (genericTypeArgumentValues != null)
            {
                // Early exit if the amount of provided generic type arguments is already enough
                if (genericTypeArgumentValues.Length == genericTypeArgumentsAmount)
                    return genericTypeArgumentValues.Select(x => x.Type).ToArray();

                for (int i = 0; i < valueCount && i < genericTypeArgumentsAmount; i++)
                    normalizedGenericTypeValues[i] = genericTypeArgumentValues[i];
            }

            // If no, or not enough generic types were passed, try to infer them from the parameter value types
            if (normalizedGenericTypeValues == null || valueCount != genericTypeArgumentsAmount)
            {
                // If some generic types were passed, start checking after the passed parameters.
                int parameterIndex = valueCount;

                for (int genericTypeIndex = parameterIndex; genericTypeIndex < genericTypeArgumentsAmount && parameterIndex < parameterTypes.Count; genericTypeIndex++)
                {
                    normalizedGenericTypeValues[genericTypeIndex] = new GenericType(parameterTypes[parameterIndex]);

                    parameterIndex++;
                }

                if (normalizedGenericTypeValues.Any(g => g == null))
                    throw new TargetInvocationException(new Exception($"The method requires {genericTypeArgumentsAmount} generic arguments"));
            }

            return normalizedGenericTypeValues.Select(g => g?.Type).ToArray();
        }
    }
}
