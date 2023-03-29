using GmodNET.API;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Stores references to instances/objects that can't live in Lua. Can later be retrieved by their ID.
    /// </summary>
    public class InstanceRepository
    {
        /// <summary>
        /// Name of the key in the class table where the type name (string) of a class is stored.
        /// </summary>
        public const string KEY_CLASS_TYPE = "__GmodMongoDbType";

        /// <summary>
        /// Name of the key in the instance metatable where the id (string) of the instance is stored.
        /// </summary>
        public const string KEY_INSTANCE_ID = "__GmodMongoDbInstanceId";

        /// <summary>
        /// Name of the key in the instance table where the type name (string) of an instance is stored.
        /// </summary>
        public const string KEY_INSTANCE_TYPE = "__GmodMongoDbInstanceType";

        /// <summary>
        /// Name of the key in the registry table where the metatables for instances are stored.
        /// </summary>
        public const string KEY_TYPE_META_TABLES = "__GmodMongoDbInstanceMetaTables";

        /// <summary>
        /// Map of instance id's to the referenced instances.
        /// </summary>
        private readonly Dictionary<string, object> instanceIds;

        /// <summary>
        /// Gets the instance ID of the instance on the stack in the Lua environment.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public static string GetInstanceId(ILua lua)
        {
            if (!lua.IsType(-1, TYPES.TABLE))
                return null;

            lua.GetField(-1, KEY_INSTANCE_ID);

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
        /// Gets the registry key for a type where the metatable will be stored.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes all type metatables in the Lua environment to clear references.
        /// </summary>
        /// <param name="lua"></param>
        public static void CleanTypeMetaTables(ILua lua)
        {
            // Clean up the instance repository
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG);
            lua.PushNil();
            lua.SetField(-2, KEY_TYPE_META_TABLES);
            lua.Pop(); // Pop the registry
        }

        /// <summary>
        /// Constructors a new instance repository.
        /// </summary>
        public InstanceRepository() 
        {
            instanceIds = new();
        }

        /// <summary>
        /// Removes all added helper functions, metatables and lingering references.
        /// </summary>
        /// <remarks>
        /// Note that a MongoCLient must have it's cluster closed manually and well before cleanup. 
        /// If it's still connected it may keep a reference incorrectly and cause the module to fail to unload.
        /// </remarks>
        /// <param name="lua"></param>
        public async void Cleanup(ILua lua)
        {
            CleanTypeMetaTables(lua);
            UnregisterHelpers(lua);

            foreach (var instance in instanceIds.Values)
            {

                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (instance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    if (instance is MongoClient client) 
                    {
                        client.Cluster.Dispose();

                        // Wait a bit for the cluster to dispose
                        Thread.Sleep(2000); // Additionally the user should manually dispose the cluster
                    }
                }
            }

            instanceIds.Clear();
        }

        /// <summary>
        /// Stores an instance in the registry for later retrieval, returns a unique id by which it can be retrieved.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns>Unique id by which it can be retrieved</returns>
        public string RegisterInstance(object instance)
        {
            var instanceId = Guid.NewGuid().ToString();

            instanceIds.Add(instanceId, instance);

            return instanceId;
        }

        /// <summary>
        /// Retrieves the instance of an object by it's ID.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>The instance or null if it could not be found in the registry.</returns>
        public object GetInstanceById(string instanceId)
        {
            if (!instanceIds.TryGetValue(instanceId, out var instance))
                return null;

            return instance;
        }

        /// <summary>
        /// Checks if the table on top of the stack is an instance.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns></returns>
        public bool IsInstance(ILua lua)
        {
            var id = GetInstanceId(lua);

            return id != null && GetInstanceById(id) != null;
        }

        /// <summary>
        /// Registers helpful Lua functions and constants into the Lua environment.
        /// </summary>
        /// <param name="lua"></param>
        public void Setup(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table

            // Global constants
            lua.PushString(KEY_CLASS_TYPE);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}{nameof(KEY_CLASS_TYPE)}");
            lua.PushString(KEY_INSTANCE_ID);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}{nameof(KEY_INSTANCE_ID)}");
            lua.PushString(KEY_INSTANCE_TYPE);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}{nameof(KEY_INSTANCE_TYPE)}");
            lua.PushString(KEY_TYPE_META_TABLES);
            lua.SetField(-2, $"{GmodMongoDb.Setup.CONSTANT_PREFIX}{nameof(KEY_TYPE_META_TABLES)}");

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

        /// <summary>
        /// Unregisters the helpers from the Lua environment.
        /// </summary>
        /// <param name="lua"></param>
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
        /// Pulls the instance from the Lua stack and returns it to C#.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        public object PullInstance(ILua lua, int stackPos = -1)
        {
            lua.Push(stackPos); // copy the instance to the top
            var instanceId = GetInstanceId(lua) ?? throw new Exception("Cannot pull instance! Expected a table with an instance id");
            lua.Pop(); // Pop the copy
            lua.Remove(stackPos); // remove the original
            return GetInstanceById(instanceId);
        }

        /// <summary>
        /// Pushes an instance to Lua.
        /// 
        /// This creates a table for the object, assigning the appropriate type metatable and keeping a reference to the object pointer.
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
        /// Creates a metatable for the given type and puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type"></param>
        public void CreateTypeMetaTable(ILua lua, Type type)
        {
            lua.CreateTable();
            lua.PushString(RegisterInstance(type));
            lua.SetField(-2, KEY_CLASS_TYPE);
            lua.Push(-1);
            lua.SetField(-2, "__index");
        }

        /// <summary>
        /// Checks if the table on top of the stack is a type metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        public bool IsTypeMetaTable(ILua lua, int stackPos = -1)
        {
            if (!lua.IsType(stackPos, TYPES.TABLE))
                return false;

            lua.GetField(stackPos, KEY_CLASS_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = GetInstanceById(instanceId);

            return instance != null;
        }

        /// <summary>
        /// Gets the type stored with the metatable.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        public Type GetTypeMetaTableType(ILua lua, int stackPos = -1)
        {
            if (!IsTypeMetaTable(lua, stackPos))
                return null;

            lua.GetField(stackPos, KEY_CLASS_TYPE);
            var instanceId = lua.GetString(-1);
            lua.Pop(); // Pop the instance id

            var instance = GetInstanceById(instanceId);

            return instance as Type;
        }

        /// <summary>
        /// Pushes a metatable onto the stack for this type (fetching it from the registry). 
        /// It creates a new metatable if it doesn't exist yet.
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
