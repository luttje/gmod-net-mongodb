using Amazon.Runtime;
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
    public class DynamicWrapper : IDisposable
    {
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

            lua.RegisterHelpers();

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
        /// Registers a type in sub-tables for each namespace part (seperated by dots)
        /// </summary>
        /// <param name="type"></param>
        public void RegisterType(Type type)
        {
            GetTypeTable(type, out var lastPartName); // Type table

            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .DistinctBy(m => m.Name);

            foreach (var member in members)
            {
                switch (member)
                {
                    case ConstructorInfo constructor:
                        SetConstructorManagedMethod(constructor, type);
                        break;
                    case MethodInfo method when method.IsStatic:
                        SetStaticManagedMethod(method, type);
                        break;
                    case MethodInfo method:
                        SetManagedMethod(method, type);
                        break;
                    case PropertyInfo property:
                        SetManagedProperty(property, type);
                        break;
                    case FieldInfo field:
                        SetManagedField(field, type);
                        break;
                    default:
                        //Console.WriteLine($"Member {member.Name} is currently not supported ({member.MemberType})");
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
            lua.Pop(); // Pop the global table

            lua.CleanTypeMetaTables();
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="anyMethod"></param>
        /// <param name="type"></param>
        private void SetStaticManagedMethod(MethodInfo anyMethod, Type type)
        {
            lua.PushManagedFunctionWrapper(type, anyMethod.Name, true);
            lua.SetField(-2, anyMethod.Name); // Type method
        }

        /// <summary>
        /// Gets or creates a metatable and adds this method to it. It will later be used
        /// as the metatable for instances of this constructor.
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="anyMethod"></param>
        /// <param name="type"></param>
        private void SetManagedMethod(MethodInfo anyMethod, Type type)
        {
            lua.PushTypeMetatable(type);
            lua.PushManagedFunctionWrapper(type, anyMethod.Name);
            lua.SetField(-2, anyMethod.Name); // Type method
            
            if (anyMethod.Name == "ToString")
            {
                lua.GetField(-1, anyMethod.Name); // Type method
                lua.SetField(-2, "__tostring");
            }

            lua.Pop(); // Pop the instance meta table
        }

        /// <summary>
        /// Sets a function to return a table with the metatable.
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="anyConstructor"></param>
        /// <param name="type"></param>
        private void SetConstructorManagedMethod(ConstructorInfo anyConstructor, Type type)
        {
            lua.CreateTypeMetaTable(type);
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top();
                var parameters = new object[upValueCount - 1];
                GenericType[] genericArguments = null;

                for (int i = 1; i < upValueCount; i++)
                {
                    var index = parameters.Length - i;
                    var parameterValue = parameters[index] = lua.PullType();

                    if (parameterValue is GenericType)
                    {
                        if (genericArguments == null)
                            genericArguments = new GenericType[upValueCount - parameters.Length];

                        genericArguments[index] = (GenericType)parameterValue;
                    }
                }

                // Remove the generic types from the parameters
                if (genericArguments != null)
                {
                    parameters = parameters.Where(p => !(p is GenericType)).ToArray();
                }
                
                // Pop the table itself which is the first argument passed to __call (and thus lowest on the stack)
                lua.Pop();

                // Handle types that have generic parameters
                if (type.ContainsGenericParameters)
                {
                    var genericTypes = genericArguments.Select(g => g.Type).ToArray();
                    type = type.MakeGenericType(genericTypes);
                }

                var parameterTypes = parameters.Select(p => p.GetType()).ToList();
                var constructor = type.GetAppropriateConstructor(ref parameterTypes);

                if (constructor == null)
                {
                    var signatures = type.GetConstructorSignatures();
                    var types = string.Join(", ", parameterTypes);
                    throw new Exception($"Incorrect parameters passed to {type.Namespace}.{type.Name} Constructor! {parameters.Length} parameters were passed (of types {types}), but only the following overloads exist: \n{signatures}");
                }

                try
                {
                    parameters = TypeTools.NormalizeParameters(parameters, constructor.GetParameters());

                    var instance = constructor.Invoke(parameters);
                    lua.PushInstance(instance);

                    return 1;
                }
                catch (NotSupportedException e)
                {
                    throw new Exception($"The constructor for {type.Namespace}.{type.Name} cannot be called this way!", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while calling {type.Namespace}.{type.Name}.{constructor.Name}!", e);
                }
            });
            lua.SetField(-2, "__call"); // Constructor method
            lua.SetMetaTable(-2);
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        private void SetManagedProperty(PropertyInfo property, Type type)
        {
            lua.PushTypeMetatable(type, TypeMetaSubTables.Properties);
            lua.PushManagedFunction((lua) =>
            {
                var instance = lua.PullInstance();
                var instanceProperty = instance.GetType().GetProperty(property.Name);
                var value = instanceProperty.GetValue(instance);
                lua.PushType(value);

                return 1;
            });
            lua.SetField(-2, property.Name); // Type property getter
            lua.Pop(); // Pop the instance meta table
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="type"></param>
        private void SetManagedField(FieldInfo field, Type type)
        {
            lua.PushTypeMetatable(type, TypeMetaSubTables.Fields);
            lua.PushManagedFunction((lua) =>
            {
                var instance = lua.PullInstance();
                var instanceField = instance.GetType().GetField(field.Name);
                var value = instanceField.GetValue(instance);
                lua.PushType(value);

                return 1;
            });
            lua.SetField(-2, field.Name); // Type field getter
            lua.Pop(); // Pop the instance meta table
        }
    }
}
