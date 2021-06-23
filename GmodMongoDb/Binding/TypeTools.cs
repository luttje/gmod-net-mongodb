using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helps converting between .NET objects/types and Lua types
    /// </summary>
    public static class TypeTools
    {
        private static readonly Dictionary<int, Type> MetaTableTypeIds = new();
        private static readonly Dictionary<Type, Type> TransformerTypes = new();
        private static string[] metaTableTypeNames = null;

        /// <summary>
        /// Iterates all Transformers and registers their Type for later use. In order to create a transformer you should inherit LuaValueTransformer
        /// </summary>
        internal static void DiscoverDataTransformers()
        {
            var transformerBaseType = typeof(BaseLuaValueTransformer);
            var discoveredTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(transformerBaseType));

            foreach (var transformerType in discoveredTypes)
            {
                var genericArguments = transformerType.BaseType.GetGenericArguments();

                TransformerTypes.Add(genericArguments[0], transformerType);
            }
        }

        /// <summary>
        /// Eagerly search for metatable definitions and create the metatables.
        /// </summary>
        /// <param name="lua"></param>
        internal static void CreateDiscoveredMetaTableDefinitions(ILua lua)
        {
            var metaTableBaseType = typeof(LuaMetaObjectBinding);
            var discoveredTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(metaTableBaseType));
            metaTableTypeNames = new string[discoveredTypes.Count()];
            var i = 0;

            foreach (var metaTableType in discoveredTypes)
            {
                var metaTableName = GetMetaTableTypeName(metaTableType);
                var typeId = lua.CreateMetaTable(metaTableName);

                if (!MetaTableTypeIds.ContainsKey(typeId))
                    MetaTableTypeIds.Add(typeId, metaTableType);

                lua.Push(-1);
                lua.SetField(-2, "__index");

                // Have a place for this type's static methods
                lua.CreateTable();
                lua.Push(-1);
                lua.Insert(1); // Move mt._static to the bottom of the stack so we can fill it later
                // Add the static method table currently at the top of the stack to the global table
                lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                lua.Insert(-2);
                lua.SetField(-2, metaTableName);
                metaTableTypeNames[i] = metaTableName;
                lua.Pop(1); // pop the global table

                MethodInfo[] methods = metaTableType.GetMethods();

                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes<LuaMethodAttribute>()
                        .Union(method.GetCustomAttributes<LuaStaticAttribute>());

                    foreach (var attribute in attributes)
                    {
                        var stackPos = -2; // Position of the metatable
                        attribute.PushFunction(lua, method, typeId);

                        if (attribute is LuaStaticAttribute staticAttribute)
                        {
                            stackPos = 1; // Position of the static table

                            if (staticAttribute.IsInitializer)
                            {
                                // TODO: Test what happens with overloaded initializers
                                // Create a metatable for the static table and add the __call metamethod to it
                                lua.CreateTable();
                                staticAttribute.PushFunction(lua, method);
                                lua.SetField(-2, "__call");
                                lua.SetMetaTable(stackPos); // Pops the metatable for the static table

                                // TODO: Document StaticTableName() in README
                            }
                        }

                        var methodName = attribute.Name ?? method.Name;
                        lua.SetField(stackPos, methodName);
                    }
                }

                lua.Pop(1); // Pop the metatable
                lua.Pop(1); // Pop the static functions table
            }
        }

        /// <summary>
        /// Cleans up the static tables made with <see cref="CreateDiscoveredMetaTableDefinitions"/>
        /// </summary>
        /// <param name="lua"></param>
        internal static void CleanUpStaticFunctionTables(ILua lua)
        {
            if (metaTableTypeNames == null)
                return;

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);

            foreach (var metaTableName in metaTableTypeNames)
            {
                if (metaTableName == null)
                    continue;

                lua.PushNil();
                lua.SetField(-2, metaTableName);
            }

            lua.Pop(1); // pop the global table
        }

        private static string GetMetaTableTypeName(Type type)
        {
            var classAttribute = type.GetCustomAttribute<LuaMetaTableAttribute>();
            return classAttribute?.Name ?? type.Name;
        }

        /// <summary>
        /// Generates userdata with a metatable by looking at attributes on the given object. Pushes the userdata onto the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="instance">The object to generate into userdata</param>
        /// <returns>The metatable type id that was created for this userdata</returns>
        public static int GenerateUserDataFromObject(ILua lua, LuaMetaObjectBinding instance)
        {
            int instanceTypeId = PushManagedObject(lua, instance);
            
            return instanceTypeId;
        }

        /// <summary>
        /// Pushes a .NET managed object onto the stack as userdata.
        /// 
        /// Consider using <see cref="GenerateUserDataFromObject"/> if you want to interact with the userdata from Lua.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="managed">The object to convert to userdata and push</param>
        /// <returns>Returns the metatable type id which was applied to the userdata</returns>
        public static int PushManagedObject(ILua lua, object managed)
        {
            var handle = GCHandle.Alloc(managed, GCHandleType.Weak);
            ReferenceManager.Add(handle);

            // CreateMetaTable will automatically find an existing metatable with this type name, so no need to look in MetaTableTypeIds for it. 
            string typeName = GetMetaTableTypeName(managed.GetType());
            var typeId = lua.CreateMetaTable(typeName);
            lua.Pop(1);

            lua.PushUserType((IntPtr)handle, typeId);

            return typeId;
        }

#nullable enable
        /// <summary>
        /// Pull userdata from the stack at the given position that has the given type id. Converts it to a .NET object.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="typeId">The metatable type id of the userdata as returned by <see cref="PushManagedObject"/> or <see cref="GenerateUserDataFromObject"/></param>
        /// <param name="stackPos">The position of the managed object</param>
        /// <param name="forceKeepOnStack">Keep the object on the stack, false to remove it</param>
        /// <returns>The .NET object converted from Lua</returns>
        /// <exception cref="NullReferenceException">Thrown when a null pointer is found or the userdata is no longer valid</exception>
        public static object PullManagedObject(ILua lua, int typeId, int stackPos = -1, bool forceKeepOnStack = false)
        {
            var handle = lua.GetUserType(stackPos, typeId);

            if(!forceKeepOnStack)
                lua.Remove(stackPos); // Remove the object

            if (handle == IntPtr.Zero)
                throw new NullReferenceException("Invalid instance pointer!");

            object? reference = GCHandle.FromIntPtr(handle).Target;

            if (reference == null)
                throw new NullReferenceException("Userdata reference has gone away!");

            return reference;
        }
#nullable disable
        
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
            else if (type == typeof(LuaFunctionReference) || type == typeof(LuaReference))
            {
                var reference = value as LuaReference;
                reference.Push();

                return 1;
            }
            else if (TransformerTypes.ContainsKey(type))
                return lua.ApplyTransformerConvert(TransformerTypes[type], value);
            else
                // TODO
                throw new NotImplementedException($"This type is not registered for conversion to Lua from .NET! Consider building a Transformer. Type is: {type.FullName}");

            return 0;
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
            else if (type == typeof(LuaFunctionReference))
            {
                value = new LuaFunctionReference(lua, stackPos, forceKeepOnStack);
                pop = false;
            }
            else if (type == typeof(LuaTableReference))
            {
                value = new LuaTableReference(lua, stackPos, forceKeepOnStack);
                pop = false;
            }
            else if (type == typeof(LuaReference))
            {
                value = new LuaReference(lua, stackPos, forceKeepOnStack);
                pop = false;
            }
            // Handle managed objects before trying the transformers
            else if (TryGetMetaTableType(lua.GetType(stackPos), out Type metaTableType) && metaTableType == type)
            {
                value = PullManagedObject(lua, lua.GetType(stackPos), stackPos, forceKeepOnStack);
                pop = false;
            }
            // Find a transformer to do the work
            else if (TransformerTypes.ContainsKey(type))
            {
                var transformerType = TransformerTypes[type];
                if (!lua.ApplyTransformerParse(transformerType, out value, stackPos, forceKeepOnStack))
                    throw new InvalidCastException($"Could not apply transformer {transformerType} to get value from Lua stack at stackpos {stackPos}");
                pop = false;
            }
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
                TYPES.FUNCTION => typeof(LuaFunctionReference),
                TYPES.TABLE => typeof(LuaTableReference),
                TYPES.NIL => null,
                _ => typeof(LuaReference),
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
