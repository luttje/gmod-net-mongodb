using GmodMongoDb.Util;
using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helpful functions to debug or message in Lua.
    /// </summary>
    public static class LuaExtensions
    {
        /// <summary>
        /// Prints a message in Lua
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="message">The message to show</param>
        public static void Print(this ILua lua, object message)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "print");
            lua.PushString(message.ToString());
            lua.MCall(1, 0);
            lua.Pop(1);
        }

        /// <summary>
        /// Prints a table in Lua
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        public static void PrintTable(this ILua lua, int stackPos = -1)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "PrintTable");
            lua.Push(stackPos - 2); // -2 to skip the PrintTable function and the global table
            lua.MCall(1, 0);
            lua.Pop(1); // Pop the global table
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
                    TYPES.TABLE => $"{lua.GetTableJson(i)}\n",
                    _ => "POINTER\n",
                };
            }

            return $"{stack}\n";
        }

        /// <summary>
        /// Builds a string representation of a table (and its metatable) in the stack by calling the util.TableToJSON function.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        public static string GetTableJson(this ILua lua, int stackPos = -1)
        {
            // call util.TableToJSON and return the string
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "util");
            lua.GetField(-1, "TableToJSON");
            lua.Push(stackPos - 3); // copy the table
            lua.MCall(1, 1);
            var json = lua.GetString(-1);
            lua.Pop(2); // Pop the util and global table
            lua.Pop(); // Pop the json string

            // Get the metatable, if it is not nil, get the string to append to the json with |
            if (lua.GetMetaTable(-1))
            {
                // The metatable is on top of the stack
                json += $" (metatable: {GetTableJson(lua)})";
                lua.Pop(); // Pop the metatable
            }

            return json;
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="value"></param>
        public static void PushType(this ILua lua, object value)
        {
            var type = value?.GetType();

            if (!TypeTools.IsLuaType(type))
                throw new ArgumentException($"Type {type} is not a Lua type and cannot be pushed");

            TypeTools.PushType(lua, type, value);
        }

        /// <summary>
        /// Pushes a function onto the stack that redirects calls to the specified method on the specified type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="instanceRepository"></param>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="isStatic"></param>
        /// <exception cref="Exception"></exception>
        public static void PushManagedFunctionWrapper(this ILua lua, InstanceRepository instanceRepository, Type type, string methodName, bool isStatic = false)
        {
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top();
                var offset = isStatic ? 0 : 1;
                var parameterValues = new object[upValueCount - offset];
                GenericType[] genericTypeArgumentValues = null;

                for (int i = offset; i < upValueCount; i++)
                {
                    var index = parameterValues.Length - i - (1 - offset);
                    object parameterValue;

                    if (instanceRepository.IsInstance(lua))
                        parameterValue = instanceRepository.PullInstance(lua);
                    else
                        parameterValue = TypeTools.PullType(lua);

                    parameterValues[index] = parameterValue;

                    if (parameterValue is GenericType genericType)
                    {
                        genericTypeArgumentValues ??= new GenericType[upValueCount - parameterValues.Length];

                        genericTypeArgumentValues[index] = genericType;
                    }
                }

                // Remove the generic types from the parameters
                if (genericTypeArgumentValues != null)
                    parameterValues = parameterValues.Where(p => p is not GenericType).ToArray();

                var instance = !isStatic ? instanceRepository.PullInstance(lua) : null;
                var parameterTypes = parameterValues.Select(p => p.GetType()).ToList();
                MethodInfo method;
                
                if(isStatic)
                    method = type.GetAppropriateMethod(methodName, ref parameterTypes);
                else
                    method = instance.GetType().GetAppropriateMethod(methodName, ref parameterTypes);

                if (method == null)
                {
                    var signatures = type.GetMethodSignatures(methodName);
                    var types = string.Join(", ", parameterTypes);
                    throw new Exception($"Incorrect parameters passed to {type?.Namespace}.{type?.Name}.{methodName}! {parameterValues.Length} parameters were passed (of types {types}, but only the following overloads exist: \n{signatures}");
                }
                
                if (method.IsGenericMethod)
                {
                    var types = TypeTools.NormalizePossibleGenericTypeArguments(method.GetGenericArguments().Length, genericTypeArgumentValues, parameterTypes);
                    method = method.MakeGenericMethod(types);
                }

                try
                {
                    parameterValues = TypeTools.NormalizeParameters(parameterValues, method.GetParameters());

                    type.WarnIfObsolete(lua);
                    method.WarnIfObsolete(lua);
                    var result = method.Invoke(instance, parameterValues);

                    if (result != null)
                    {
                        if (TypeTools.IsLuaType(result))
                            lua.PushType(result);
                        else
                            instanceRepository.PushInstance(lua, result);
                        
                        return 1;
                    }
                }
                catch (NotSupportedException e)
                {
                    throw new Exception($"The method for {type.Namespace}.{type.Name}.{method.Name} cannot be called this way!", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while calling {type.Namespace}.{type.Name}.{method.Name}!", e);
                }

                return 0;
            });
        }
    }
}
