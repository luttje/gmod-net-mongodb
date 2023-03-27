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
    /// Helpful functions to debug or message in Lua
    /// </summary>
    public static class LuaExtensions
    {
        public const string CONSTANT_PREFIX = "GMOD_MONGODB_";
        public const string KEY_TYPE = "__GmodMongoDbType";
        public const string KEY_INSTANCE_ID = "__GmodMongoDbInstanceId";
        public const string KEY_INSTANCE_TYPE = "__GmodMongoDbInstanceType";
        public const string KEY_TYPE_META_TABLES = "__GmodMongoDbInstanceMetaTables";

        /// <summary>
        /// Registers helpful Lua functions and constants
        /// </summary>
        public static void RegisterHelpers(this ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table

            // Global constants
            lua.PushString(KEY_TYPE);
            lua.SetField(-2, $"{CONSTANT_PREFIX}KEY_TYPE");
            lua.PushString(KEY_INSTANCE_ID);
            lua.SetField(-2, $"{CONSTANT_PREFIX}KEY_INSTANCE_ID");
            lua.PushString(KEY_INSTANCE_TYPE);
            lua.SetField(-2, $"{CONSTANT_PREFIX}KEY_INSTANCE_TYPE");
            lua.PushString(KEY_TYPE_META_TABLES);
            lua.SetField(-2, $"{CONSTANT_PREFIX}KEY_TYPE_META_TABLES");

            // Global GenericType function to help construct generic types
            lua.PushManagedFunction(lua =>
            {
                if (!lua.IsTypeMetaTable())
                {
                    lua.Pop(); // Pop the parameter
                    lua.PushNil();
                    // TODO: Shouldn't we throw an invocation exception instead?
                    return 1;
                }

                var type = lua.GetTypeMetaTableType();
                lua.Pop(); // Pop the metatable parameter

                lua.PushInstance(new GenericType(type));

                return 1;
            });
            lua.SetField(-2, "GenericType");

            lua.Pop(); // Global table
        }

        public static string GetTypeRegistryKey(Type type)
        {
            var key = type.FullName;

            var genericInfoIndex = key.IndexOf('[');

            if(genericInfoIndex > -1)
            {
                key = key.Substring(0, genericInfoIndex);
            }

            return key;
        }

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
        /// <param name="index"></param>
        public static void PrintTable(this ILua lua, int index)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "PrintTable");
            lua.Push(index - 2); // -2 to skip the PrintTable function and the global table
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
                    _ => "POINTER\n",
                };
            }

            return $"{stack}\n";
        }

        /// <summary>
        /// Removes all type metatables to clear references.
        /// </summary>
        /// <param name="lua"></param>
        public static void CleanTypeMetaTables(this ILua lua)
        {
            // Clean up the instance repository
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG);
            lua.PushNil();
            lua.SetField(-2, KEY_TYPE_META_TABLES);
            lua.Pop(); // Pop the registry
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="value"></param>
        public static void PushType(this ILua lua, object value)
        {
            var type = value?.GetType();

            if (TypeTools.IsLuaType(type))
            {
                TypeTools.PushType(lua, type, value);
                return;
            }

            lua.PushInstance(value);
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static object PullType(this ILua lua, int index = -1)
        {
            if (lua.IsType(index, TYPES.TABLE))
            {
                lua.GetField(index, KEY_INSTANCE_ID);
                if (lua.IsType(-1, TYPES.STRING))
                {
                    var instanceId = lua.GetString(-1);
                    lua.Pop(); // Pop the instance id
                    lua.Pop(); // Pop the table
                    return InstanceRepository.Instance.GetInstance(instanceId);
                }
                lua.Pop();
            }

            return TypeTools.PullType(lua, index);
        }

        /// <summary>
        /// Creates a table for the object, assigning the appropriate type metatable and keeping a reference to the object pointer.
        /// Leaves the instance table on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="instance"></param>
        public static void PushInstance(this ILua lua, object instance)
        {
            var type = instance.GetType();

            lua.CreateTable(); // instance table
            lua.PushString(InstanceRepository.Instance.RegisterInstance(instance));
            lua.SetField(-2, KEY_INSTANCE_ID);
            lua.PushString(GetTypeRegistryKey(type));
            lua.SetField(-2, KEY_INSTANCE_TYPE);
            lua.PushTypeMetatable(type);

            lua.SetMetaTable(-2); // Set the metatable for the instance table
        }

        /// <summary>
        /// Pulls the instance that is on top of the stack as an object
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public static object PullInstance(this ILua lua)
        {
            var instance = lua.GetInstance();
            lua.Pop();

            return instance;
        }

        /// <summary>
        /// Gets the instance that is on top of the stack as an object.
        /// Leaves the instance table on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public static object GetInstance(this ILua lua)
        {
            // The instance table is the lowest upvalue on the stack, it's the only remaining value on the stack
            lua.GetField(-1, KEY_INSTANCE_ID);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = InstanceRepository.Instance.GetInstance(instanceId);

            if (instance == null)
                throw new Exception($"Instance ({instanceId}) was not found!");

            return instance;
        }

        /// <summary>
        /// Creates a table for the type and puts it on top of the stack. Should be used as a metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        public static void CreateTypeMetaTable(this ILua lua, Type type)
        {
            lua.CreateTable();
            lua.PushString(InstanceRepository.Instance.RegisterInstance(type));
            lua.SetField(-2, KEY_TYPE);
            lua.Push(-1);
            lua.SetField(-2, "__index");
        }

        /// <summary>
        /// Checks if the table on top of the stack is a type metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsTypeMetaTable(this ILua lua, int index = -1)
        {
            if (!lua.IsType(index, TYPES.TABLE))
                return false;
            
            lua.GetField(index, KEY_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = InstanceRepository.Instance.GetInstance(instanceId);
            
            return instance != null;
        }

        /// <summary>
        /// Gets the type stored with the metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Type GetTypeMetaTableType(this ILua lua, int index = -1)
        {
            if (!lua.IsTypeMetaTable(index))
                return null;

            lua.GetField(index, KEY_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = InstanceRepository.Instance.GetInstance(instanceId);

            return instance as Type;
        }

        /// <summary>
        /// Pushes a metatable onto the stack for this type (fetching it from the registry). It creates a new metatable if it doesn't exist yet.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        /// <param name="subTableToPush"></param>
        public static void PushTypeMetatable(this ILua lua, Type type, TypeMetaSubTables? subTableToPush = null)
        {
            var registryKey = GetTypeRegistryKey(type);

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG); // registry
            lua.GetField(-1, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES]
            if (lua.IsType(-1, TYPES.NIL))
            {
                lua.Pop(); // Pop the nil
                lua.CreateTable();
                lua.SetField(-2, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES] = {}
                lua.GetField(-1, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES]
            }
            lua.Remove(-2); // Pop the registry
            
            lua.GetField(-1, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey]

            // Create the metatable if it doesn't exist
            if (lua.IsType(-1, TYPES.NIL))
            {
                lua.Pop(); // Pop the nil
                lua.CreateTable();
                lua.SetField(-2, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey] = {}

                lua.GetField(-1, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey]

                var allSubTables = Enum.GetValues<TypeMetaSubTables>();

                foreach (var subTable in allSubTables)
                {
                    lua.CreateTable();
                    lua.SetField(-2, Enum.GetName(subTable)); // registry[KEY_INSTANCE_META_TABLES][registryKey][subTableName] = {}
                }

                // Index function to try to find the value in the sub tables, falling back to the meta table itself
                lua.PushManagedFunction((lua) =>
                {
                    var key = lua.GetString(-1);
                    lua.Pop(); // Pop the key, leaving only the instance table

                    foreach (var subTable in allSubTables)
                    {
                        lua.PushTypeMetatable(type, subTable);
                        lua.GetField(-1, key); // function to call (or nil)

                        if (!lua.IsType(-1, TYPES.NIL))
                        {
                            lua.Push(-3); // Push the instance table
                            lua.MCall(1, 1); // Call the function with the instance table as the only argument
                            lua.Remove(-2); // Remove the sub table
                            lua.Remove(-2); // Remove the instance table
                            return 1;
                        }

                        lua.Pop(2); // Pop the nil and the sub table
                    }
                    
                    lua.Remove(-1); // Remove the instance table

                    lua.PushTypeMetatable(type);
                    lua.GetField(-1, key);
                    lua.Remove(-2); // Remove the meta table

                    return 1;
                });

                lua.SetField(-2, "__index"); // pops the function

                // The default __tostring just prints the type and instance id
                lua.PushManagedFunction(lua =>
                {
                    lua.GetField(-1, KEY_INSTANCE_ID);
                    var instanceId = lua.GetString(-1);
                    lua.Pop(); // Pop the instance id
                    lua.Pop(); // Pop the instance table

                    lua.PushString($"[{registryKey}] {instanceId}");

                    return 1;
                });
                lua.SetField(-2, "__tostring"); // pops the function

            }
            lua.Remove(-2); // Pop the meta tables collection

            if (subTableToPush == null)
                return;

            lua.GetField(-1, subTableToPush.ToString());

            lua.Remove(-2); // Pop the metatable
        }

        /// <summary>
        /// Pushes a function onto the stack that redirects calls to the specified method on the specified type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="isStatic"></param>
        /// <exception cref="Exception"></exception>
        public static void PushManagedFunctionWrapper(this ILua lua, Type type, string methodName, bool isStatic = false)
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
                    var parameterValue = parameterValues[index] = lua.PullType();

                    if (parameterValue is GenericType)
                    {
                        if (genericTypeArgumentValues == null)
                            genericTypeArgumentValues = new GenericType[upValueCount - parameterValues.Length];

                        genericTypeArgumentValues[index] = (GenericType)parameterValue;
                    }
                }

                // Remove the generic types from the parameters
                if (genericTypeArgumentValues != null)
                    parameterValues = parameterValues.Where(p => !(p is GenericType)).ToArray();

                var instance = !isStatic ? lua.PullInstance() : null;
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
                        lua.PushType(result);
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
