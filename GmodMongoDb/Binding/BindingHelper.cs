﻿using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    public class BindingHelper
    {
        public static Dictionary<int, Type> MetaTableTypeIds = new Dictionary<int, Type>();

        /// <summary>
        /// Generates userdata with a metatable by looking at attributes on the given object. Pushes the userdata onto the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="instance">The object to generate into userdata</param>
        /// <returns>The metatable type id that was created for this userdata</returns>
        public static int GenerateUserDataFromObject(ILua lua, LuaMetaObjectBinding instance)
        {
            var type = instance.GetType();
            var classAttribute = type.GetCustomAttribute<LuaMetaTableAttribute>();
            var metaTableName = classAttribute?.Name ?? type.Name;

            int instanceTypeId = PushManagedObject(lua, metaTableName, instance);
            lua.PushMetaTable(instanceTypeId);

            instance.MetaTableTypeId = instanceTypeId;

            lua.Push(-1);
            lua.SetField(-2, "__index");

            MethodInfo[] methods = type.GetMethods();

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<LuaMethodAttribute>();

                foreach (var attribute in attributes)
                {
                    attribute.PushFunction(lua, method, instanceTypeId);
                    lua.SetField(-2, attribute.Name ?? method.Name);
                }
            }

            lua.Pop(1); // Pop the metatable, leaving only the userdata on the stack

            return instanceTypeId;
        }

        /// <summary>
        /// Pushes a .NET managed object onto the stack as userdata.
        /// 
        /// Consider using <see cref="GenerateUserDataFromObject"/> if you want to interact with the userdata from Lua.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="typeName">The name for the type metatable</param>
        /// <param name="managed">The object to convert to userdata and push</param>
        /// <returns>Returns the metatable type id which was applied to the userdata</returns>
        public static int PushManagedObject(ILua lua, string typeName, object managed)
        {
            var handle = GCHandle.Alloc(managed, GCHandleType.Weak);
            ReferenceManager.Add(handle);

            var typeId = lua.CreateMetaTable(typeName);
            lua.Pop(1);

            lua.PushUserType((IntPtr)handle, typeId);

            if(!MetaTableTypeIds.ContainsKey(typeId))
                MetaTableTypeIds.Add(typeId, managed.GetType());

            return typeId;
        }

#nullable enable
        /// <summary>
        /// Pull userdata from the stack at the given position that has the given type id. Converts it to a .NET object
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="typeId">The metatable type id of the userdata as returned by <see cref="PushManagedObject"/> or <see cref="GenerateUserDataFromObject"/></param>
        /// <param name="stackPos">The position of the managed object</param>
        /// <returns></returns>
        public static object? PullManagedObject(ILua lua, int typeId, int stackPos)
        {
            var handle = lua.GetUserType(stackPos, typeId);
            lua.Remove(stackPos); // Remove the object

            if (handle == IntPtr.Zero)
                throw new NullReferenceException("Invalid instance pointer!");

            object? reference = GCHandle.FromIntPtr(handle).Target;

            if (reference == null)
                throw new NullReferenceException("Userdata reference has gone away!");

            return (object)reference;
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
            else if (type == typeof(List<BsonDocument>))
                return lua.ApplyTransformerConvert(typeof(BetweenBsonDocumentListAndTable), value);
            else if (type == typeof(List<string>))
                return lua.ApplyTransformerConvert(typeof(BetweenStringListAndTable), value);
            else if (type == typeof(object[]))
                return lua.ApplyTransformerConvert(typeof(BetweenObjectArrayAndMultipleResults), value);
            else
                // TODO
                throw new NotImplementedException($"Still prototyping. TODO! Type is: {type.FullName}");

            return 0;
        }

        /// <summary>
        /// Pop a value from the Lua stack and convert it to the specified .NET type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="type">The expected type of the value on the stack</param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeep">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static object PullType(ILua lua, Type type, int stackPos, bool forceKeep = false)
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
                value = new LuaFunctionReference(lua, stackPos);
                pop = false;
            }
            else if (type == typeof(LuaTableReference))
            {
                value = new LuaTableReference(lua, stackPos);
                pop = false;
            }            
            else if (type == typeof(LuaReference))
            {
                value = new LuaReference(lua, stackPos);
                pop = false;
            }
            else // TODO
                throw new NotImplementedException($"Still prototyping. TODO! Type is: {type.FullName}");

            if (pop && !forceKeep)
                lua.Remove(stackPos);

            return value;
        }

        /// <summary>
        /// Pop a value from the Lua stack and try convert it to an associated .NET type.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeep">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static object PullType(ILua lua, int stackPos, bool forceKeep = false)
        {
            TYPES luaType = (TYPES)lua.GetType(stackPos);
            Type type = LuaTypeToDotNetType(luaType);

            return PullType(lua, type, stackPos, forceKeep);
        }

        /// <summary>
        /// Pop a value from the Lua stack and convert it to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value on the stack</typeparam>
        /// <param name="lua"></param>
        /// <param name="stackPos">The position of the value</param>
        /// <param name="forceKeep">Order the function not to pop after getting the value</param>
        /// <returns>The .NET object</returns>
        public static T PullType<T>(ILua lua, int stackPos, bool forceKeep = false)
        {
            return (T) PullType(lua, typeof(T), stackPos, forceKeep);
        }

        /// <summary>
        /// Convert a specified Lua type to a .NET type.
        /// </summary>
        /// <param name="luaType">The Lua type to convert</param>
        /// <returns>The converted .NET type</returns>
        public static Type LuaTypeToDotNetType(TYPES luaType)
        {
            if (MetaTableTypeIds.ContainsKey((int)luaType))
                return MetaTableTypeIds[(int)luaType];

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
        /// <param name="luaType">The Lua type to convert (must be castable to <see cref="TYPES"/>)</param>
        /// <returns>The converted .NET type</returns>
        public static Type LuaTypeToDotNetType(int luaType)
            => LuaTypeToDotNetType((TYPES)luaType);
    }
}
