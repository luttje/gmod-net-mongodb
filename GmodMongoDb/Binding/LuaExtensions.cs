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
        public const string KEY_INSTANCE_ID = "__GmodMongoDbInstanceId";
        public const string KEY_INSTANCE_TYPE = "__GmodMongoDbInstanceType";
        public const string KEY_TYPE_META_TABLES = "__GmodMongoDbInstanceMetaTables";

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
        /// <param name="instance"></param>
        public static void PushType(this ILua lua, object instance)
        {
            var type = instance.GetType();

            if (TypeTools.IsLuaType(type))
            {
                TypeTools.PushType(lua, type, instance);
                return;
            }

            lua.PushInstance(instance);
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
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
                    lua.Pop();
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
        /// <param name="instance"></param>
        public static void PushInstance(this ILua lua, object instance)
        {
            var type = instance.GetType();

            lua.CreateTable(); // instance table
            lua.PushString(InstanceRepository.Instance.RegisterInstance(instance));
            lua.SetField(-2, KEY_INSTANCE_ID);
            lua.PushString(type.FullName);
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
        /// Pushes a metatable onto the stack for this type (fetching it from the registry). It creates a new metatable if it doesn't exist yet.
        /// </summary>
        /// <param name="type"></param>
        public static void PushTypeMetatable(this ILua lua, Type type)
        {
            var registryKey = type.FullName;

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG); // registry
            lua.GetField(-1, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES]
            if (lua.IsType(-1, TYPES.NIL))
            {
                lua.Pop(); // Pop the nil
                lua.CreateTable(); // {}
                lua.SetField(-2, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES] = {}
                lua.GetField(-1, KEY_TYPE_META_TABLES); // registry[KEY_INSTANCE_META_TABLES]
            }
            lua.Remove(-2); // Pop the registry
            lua.GetField(-1, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey]
            if (lua.IsType(-1, TYPES.NIL))
            {
                lua.Pop(); // Pop the nil
                lua.CreateTable(); // {}
                lua.SetField(-2, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey] = {}
                lua.GetField(-1, registryKey); // registry[KEY_INSTANCE_META_TABLES][registryKey]
                lua.Push(-1); // copy registry[KEY_INSTANCE_META_TABLES][registryKey]

                // Set the __index to the metatable itself
                lua.SetField(-2, "__index"); // pops the copy

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
        }
    }
}
