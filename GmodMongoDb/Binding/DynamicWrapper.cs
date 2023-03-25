using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GmodMongoDb.Binding
{
    public class DynamicWrapper : IDisposable
    {
        public const string KEY_INSTANCE_ID = "__GmodMongoDbInstanceId";
        public const string KEY_INSTANCE_META_TABLES = "__GmodMongoDbInstanceMetaTable";
        
        private ILua lua;
        private string? baseName;

        private Dictionary<string, int> metaTableIds = new();

        /// <summary>
        /// Create a wrapper that can create wrappers for any type
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="baseName"></param>
        public DynamicWrapper(ILua lua, string? baseName = null)
        {
            this.lua = lua;
            this.baseName = baseName;

            if (baseName == null)
                return;
            
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.CreateTable(); // baseName table

            lua.SetField(-2, baseName); // baseName table
            lua.Pop(); // Pop the Global table
        }

        /// <summary>
        /// Gets or creates the Type table (and all parent tables) for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="lastPartName"></param>
        private void GetTypeTable(Type type, out string lastPartName)
        {
            var typeFullName = type.FullName;
            
            if (baseName != null)
            {
                typeFullName = typeFullName?.Substring(baseName.Length).TrimStart('.');
            }

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.GetField(-1, baseName); // MongoDB table
            lua.Remove(-2); // Pop the global table

            var parts = typeFullName.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                lua.GetField(-1, parts[i]);
                if (lua.IsType(-1, TYPES.NIL))
                {
                    lua.Pop();
                    lua.CreateTable();
                    lua.SetField(-2, parts[i]);
                    lua.GetField(-1, parts[i]);
                }

                // Remove all except the last
                if (i != parts.Length - 1)
                {
                    lua.Remove(-2);
                }
            }

            lastPartName = parts.Last();
        }

        /// <summary>
        /// Creates a table for the object, assigning the appropriate type metatable and keeping a reference to the object pointer.
        /// Leaves the instance table on top of the stack.
        /// </summary>
        /// <param name="instance"></param>
        public void PushInstance(object instance)
        {
            lua.CreateTable(); // instance table
            lua.PushString(InstanceRepository.Instance.RegisterInstance(instance));
            lua.SetField(-2, KEY_INSTANCE_ID);

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG); // registry
            lua.GetField(-1, KEY_INSTANCE_META_TABLES);
            lua.Remove(-2); // Pop the registry
            lua.GetField(-1, instance.GetType().FullName);
            lua.Remove(-2); // Pop the meta tables collection

            lua.SetMetaTable(-2); // Set the metatable for the instance table
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="instance"></param>
        public void PushType(object instance)
        {
            var type = instance.GetType();
            
            if (TypeTools.IsLuaType(type))
            {
                TypeTools.PushType(lua, type, instance);
                return;
            }

            PushInstance(instance);
        }

        /// <summary>
        /// Creates a metatable for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object PullType(int index = -1)
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
        /// Registers a type in sub-tables for each namespace part (seperated by dots)
        /// </summary>
        /// <param name="type"></param>
        public void RegisterType(Type type)
        {
            GetTypeTable(type, out var lastPartName); // Type table

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                switch (member)
                {
                    case ConstructorInfo constructor:
                        SetConstructorManagedMethod(constructor, type);
                        break;
                    case MethodInfo method when method.IsStatic:
                        SetStaticManagedMethod(method);
                        break;
                    case MethodInfo method:
                        SetManagedMethod(method, type);
                        break;
                    case PropertyInfo property:
                        //SetManagedProperty(property, type);
                        break;
                    case FieldInfo field:
                        //SetManagedField(field, type);
                        break;
                    default:
                        //Console.WriteLine($"Member {member.Name} is not supported ({member.MemberType})");
                        break;
                        //default:
                        //throw new NotImplementedException($"{member.GetType()} is not a supported member type yet for DynamicWrapper.");
                }
            }
            
            lua.Pop(); // Pop the type table

            lua.Pop(); // Pop the MongoDB table
        }

        public void Dispose()
        {
            metaTableIds.Clear();

            if (baseName == null)
                return;
            
            // Set MongoDB to nil to release all references to the types.
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, baseName);
            lua.Pop();
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        private void SetStaticManagedMethod(MethodInfo method)
        {
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top();
                var parameters = new object[upValueCount];

                for (int i = 1; i <= upValueCount; i++)
                {
                    var index = upValueCount - i;
                    parameters[index] = PullType();
                    Console.WriteLine($"Upvalue {index}: {parameters[index]}");
                }

                try
                {
                    var result = method.Invoke(null, parameters);

                    if (result != null)
                    {
                        PushType(result);
                        return 1;
                    }
                }
                catch (TargetParameterCountException e)
                {
                    throw new Exception($"Incorrect parameters passed to {method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}! {parameters.Length} parameters were passed, but {method.GetParameters().Length} were expected.", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while calling {method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}!", e);
                }
                
                return 0;
            });
            lua.SetField(-2, method.Name); // Type method
        }

        /// <summary>
        /// Gets or creates a metatable and adds this method to it. It will later be used
        /// as the metatable for instances of this constructor.
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="method"></param>
        private void SetManagedMethod(MethodInfo method, Type type)
        {
            // Gets or creates the KEY_INSTANCE_META_TABLE table for this type
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_REG); // registry
            lua.GetField(-1, KEY_INSTANCE_META_TABLES);
            if (lua.IsType(-1, TYPES.NIL))
            {
                lua.Pop();
                lua.CreateTable();
                lua.SetField(-2, KEY_INSTANCE_META_TABLES);
                lua.GetField(-1, KEY_INSTANCE_META_TABLES);
            }
            lua.Remove(-2); // Pop the registry

            // Push the method
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top();
                var parameters = new object[upValueCount - 1];

                for (int i = 1; i < upValueCount - 1; i++)
                {
                    var index = upValueCount - i;
                    parameters[index] = PullType();
                    Console.WriteLine($"Upvalue {index}: {parameters[index]}");
                }

                // The instance table is the lowest upvalue on the stack, it's the only remaining value on the stack
                lua.GetField(-1, KEY_INSTANCE_ID);
                var instanceId = lua.GetString(-1);
                lua.Pop(); // Pop the instance id
                lua.Pop(); // Pop the instance table (we don't need it anymore)
                var instance = InstanceRepository.Instance.GetInstance(instanceId);

                if (instance == null)
                    throw new Exception($"Instance ({instanceId}) was not found while calling {method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}!");

                try
                {
                    var result = method.Invoke(instance, parameters);

                    if (result != null)
                    {
                        PushType(result);
                        return 1;
                    }
                }
                catch (TargetParameterCountException e)
                {
                    throw new Exception($"Incorrect parameters passed to {method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}! {parameters.Length} parameters were passed, but {method.GetParameters().Length} were expected.", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while calling {method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name}!", e);
                }

                return 0;
            });
            lua.SetField(-2, method.Name); // Type method

            lua.Pop(); // Pop the instance meta table
        }

        /// <summary>
        /// Sets a function to return a table with the metatable.
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="constructor"></param>
        /// TODO: Support constructor overloading
        private void SetConstructorManagedMethod(ConstructorInfo constructor, Type type)
        {
            lua.CreateTable();
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top() - 1;
                var parameters = new object[upValueCount];

                for (int i = 1; i <= upValueCount; i++)
                {
                    var index = upValueCount - i;
                    parameters[index] = PullType();
                    Console.WriteLine($"Upvalue {index}: {parameters[index]}");
                }

                // Pop the table itself which is the first argument passed to __call (and thus lowest on the stack)
                lua.Pop();

                try
                {
                    if (parameters.Length != constructor.GetParameters().Length)
                        throw new TargetParameterCountException();

                    var instance = constructor.Invoke(TypeTools.NormalizeParameters(parameters, constructor.GetParameters()));
                    PushInstance(instance);

                    lua.Print(lua.GetStack());
                    return 1;
                }
                catch (TargetParameterCountException e)
                {
                    throw new Exception($"Incorrect parameters passed to {constructor.DeclaringType?.Namespace}.{constructor.DeclaringType?.Name}.{constructor.Name}! {parameters.Length} parameters were passed, but {constructor.GetParameters().Length} were expected.", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while calling {constructor.DeclaringType?.Namespace}.{constructor.DeclaringType?.Name}.{constructor.Name}!", e);
                }
            });
            lua.SetField(-2, "__call"); // Constructor method
            lua.SetMetaTable(-2);
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        private void SetManagedProperty(PropertyInfo property, Type type)
        {
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        private void SetManagedField(FieldInfo field, Type type)
        {
        }
    }
}
