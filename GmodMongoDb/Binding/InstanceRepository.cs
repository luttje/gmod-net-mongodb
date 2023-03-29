using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    public class InstanceRepository
    {
        public const string KEY_TYPE = "__GmodMongoDbType";
        public const string KEY_INSTANCE_ID = "__GmodMongoDbInstanceId";
        public const string KEY_INSTANCE_TYPE = "__GmodMongoDbInstanceType";
        public const string KEY_TYPE_META_TABLES = "__GmodMongoDbInstanceMetaTables";

        private Dictionary<string, object> instanceIds;

        public static string GetTypeRegistryKey(Type type)
        {
            var key = type.FullName;

            var genericInfoIndex = key.IndexOf('[');

            if (genericInfoIndex > -1)
            {
                key = key.Substring(0, genericInfoIndex);
            }

            return key;
        }

        public InstanceRepository() 
        {
            instanceIds = new();
        }

        public void Cleanup(ILua lua)
        {
            CleanTypeMetaTables(lua);
            UnregisterHelpers(lua);
            instanceIds.Clear();
        }
        
        public string RegisterInstance(object instance)
        {
            var instanceId = Guid.NewGuid().ToString();

            instanceIds.Add(instanceId, instance);

            return instanceId;
        }

        public object GetInstance(string instanceId)
        {
            if (!instanceIds.TryGetValue(instanceId, out var instance))
                return null;

            return instance;
        }

        public string? GetInstanceId(ILua lua, int stackPos)
        {
            if (!lua.IsType(stackPos, TYPES.TABLE))
                return null;

            lua.GetField(stackPos, KEY_INSTANCE_ID);

            if (!lua.IsType(-1, TYPES.STRING))
            {
                lua.Pop(); // Pop the instance id
                return null;
            }

            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id
            return instanceId;
        }

        /// <summary>
        /// Removes all type metatables to clear references.
        /// </summary>
        /// <param name="lua"></param>
        public void CleanTypeMetaTables(ILua lua)
        {
            // Clean up the instance repository
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG);
            lua.PushNil();
            lua.SetField(-2, KEY_TYPE_META_TABLES);
            lua.Pop(); // Pop the registry
        }

        /// <summary>
        /// Registers helpful Lua functions and constants
        /// </summary>
        public void Setup(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table

            // Global constants
            lua.PushString(KEY_TYPE);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_TYPE");
            lua.PushString(KEY_INSTANCE_ID);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_INSTANCE_ID");
            lua.PushString(KEY_INSTANCE_TYPE);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_INSTANCE_TYPE");
            lua.PushString(KEY_TYPE_META_TABLES);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_TYPE_META_TABLES");

            // Global GenericType function to help construct generic types
            lua.PushManagedFunction(lua =>
            {
                // TODO: Throw ArgumentException on lua.Top() not being 1 (create helper for type checking to reuse)

                if (!IsTypeMetaTable(lua))
                {
                    lua.Pop(); // Pop the parameter
                    lua.PushNil();
                    // TODO: Shouldn't we throw an invocation exception instead?
                    return 1;
                }

                var type = GetTypeMetaTableType(lua);
                lua.Pop(); // Pop the metatable parameter

                PushInstance(lua, new GenericType(type));

                return 1;
            });
            lua.SetField(-2, "GenericType");

            lua.Pop(); // Global table
        }

        public void UnregisterHelpers(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table

            // Global constants
            lua.PushNil();
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_TYPE");
            lua.PushNil();
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_INSTANCE_ID");
            lua.PushNil();
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_INSTANCE_TYPE");
            lua.PushNil();
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}KEY_TYPE_META_TABLES");

            // Global GenericType function
            lua.PushNil();
            lua.SetField(-2, "GenericType");

            lua.Pop(); // Global table
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        public object PullInstance(ILua lua, int stackPos = -1)
        {
            var instanceId = GetInstanceId(lua, stackPos);

            if (instanceId == null)
                throw new Exception("Cannot pull instance! Expected a table with an instance id");

            lua.Pop(); // Pop the table
            return GetInstance(instanceId);
        }

        /// <summary>
        /// Creates a table for the object, assigning the appropriate type metatable and keeping a reference to the object pointer.
        /// Leaves the instance table on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="instance"></param>
        public void PushInstance(ILua lua, object instance)
        {
            var type = instance.GetType();

            lua.CreateTable(); // instance table
            lua.PushString(RegisterInstance(instance));
            lua.SetField(-2, KEY_INSTANCE_ID);
            lua.PushString(GetTypeRegistryKey(type));
            lua.SetField(-2, KEY_INSTANCE_TYPE);
            PushTypeMetatable(lua, type);

            lua.SetMetaTable(-2); // Set the metatable for the instance table
        }

        /// <summary>
        /// Pulls the instance that is on top of the stack as an object
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public object PullInstance(ILua lua)
        {
            var instance = GetInstance(lua);
            lua.Pop();

            return instance;
        }

        /// <summary>
        /// Gets the instance that is on top of the stack as an object.
        /// Leaves the instance table on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public object GetInstance(ILua lua)
        {
            // The instance table is the lowest upvalue on the stack, it's the only remaining value on the stack
            lua.GetField(-1, KEY_INSTANCE_ID);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = GetInstance(instanceId);

            if (instance == null)
                throw new Exception($"Instance ({instanceId}) was not found!");

            return instance;
        }

        /// <summary>
        /// Creates a table for the type and puts it on top of the stack. Should be used as a metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        public void CreateTypeMetaTable(ILua lua, Type type)
        {
            lua.CreateTable();
            lua.PushString(RegisterInstance(type));
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
        public bool IsTypeMetaTable(ILua lua, int index = -1)
        {
            if (!lua.IsType(index, TYPES.TABLE))
                return false;

            lua.GetField(index, KEY_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = GetInstance(instanceId);

            return instance != null;
        }

        /// <summary>
        /// Gets the type stored with the metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Type GetTypeMetaTableType(ILua lua, int index = -1)
        {
            if (!IsTypeMetaTable(lua, index))
                return null;

            lua.GetField(index, KEY_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = GetInstance(instanceId);

            return instance as Type;
        }

        /// <summary>
        /// Pushes a metatable onto the stack for this type (fetching it from the registry). It creates a new metatable if it doesn't exist yet.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        /// <param name="subTableToPush"></param>
        public void PushTypeMetatable(ILua lua, Type type, TypeMetaSubTables? subTableToPush = null)
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
                        PushTypeMetatable(lua, type, subTable);
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

                    PushTypeMetatable(lua, type);
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
    }
}
