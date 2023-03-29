using GmodMongoDb.Util;
using GmodNET.API;
using System;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Wrapper that binds a managed type's constructors, methods, properties, and fields to Lua.
    /// </summary>
    public class DynamicWrapper : IDisposable
    {
        /// <summary>
        /// A reference to the Lua environment provided to the constructor.
        /// </summary>
        private readonly ILua lua;

        /// <summary>
        /// The name of the table of which all types will be (extended) children of.
        /// </summary>
        private readonly string baseName;

        /// <summary>
        /// A storage for managed object instances.
        /// </summary>
        private InstanceRepository instanceRepository = new();

        /// <summary>
        /// Create a wrapper that can create bindings for any given type
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="baseName"></param>
        public DynamicWrapper(ILua lua, string baseName = null)
        {
            this.lua = lua;
            this.baseName = baseName;

            instanceRepository.Setup(lua);

            if (baseName == null)
                return;
            
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.CreateTable(); // baseName table

            lua.SetField(-2, baseName); // baseName table
            lua.Pop(); // Pop the Global table
        }

        /// <summary>
        /// Cleans up any handles and references by removing tables and functions.
        /// </summary>
        public void Dispose()
        {
            instanceRepository.Cleanup(lua);
            instanceRepository = null;

            if (baseName == null)
                return;

            // Set MongoDB to nil to release all references to the types.
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, baseName);
            lua.Pop(); // Pop the global table

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or creates the Type table (and all parent tables) for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="lastPartName"></param>
        private void GetTypeTable(Type type, out string lastPartName)
        {
            var typeIsGeneric = type.IsGenericType;
            var typeFullName = type.FullName;
            
            if (baseName != null)
                typeFullName = typeFullName?.Substring(baseName.Length).TrimStart('.');

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.GetField(-1, baseName); // MongoDB table
            lua.Remove(-2); // Pop the global table

            var parts = typeFullName.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if(typeIsGeneric && i == parts.Length - 1)
                    part = part.Substring(0, part.IndexOf('`'));
                
                lua.GetField(-1, part);
                if (lua.IsType(-1, TYPES.NIL))
                {
                    lua.Pop();
                    lua.CreateTable();
                    lua.SetField(-2, part);
                    lua.GetField(-1, part);
                }

                // Remove all except the last
                if (i != parts.Length - 1)
                    lua.Remove(-2);
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

        /// <summary>
        /// Registers all types in the provided assembly, if their namespace is not null.
        /// </summary>
        /// <param name="assemblies"></param>
        public void RegisterTypes(Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null);

                foreach (var type in types)
                {
                    RegisterType(type);
                }
            }
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="anyMethod"></param>
        /// <param name="type"></param>
        private void SetStaticManagedMethod(MethodInfo anyMethod, Type type)
        {
            lua.PushManagedFunctionWrapper(instanceRepository, type, anyMethod.Name, true);
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
            instanceRepository.PushTypeMetatable(lua, type); // Instance meta table
            lua.PushManagedFunctionWrapper(instanceRepository, type, anyMethod.Name);
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
            instanceRepository.CreateTypeMetaTable(lua, type); // Type meta table
            lua.PushManagedFunction((lua) =>
            {
                var upValueCount = lua.Top();
                var parameterValues = new object[upValueCount - 1];
                GenericType[] genericArguments = null;

                for (int i = 1; i < upValueCount; i++)
                {
                    var index = parameterValues.Length - i;
                    object parameterValue;

                    if (instanceRepository.IsInstance(lua))
                        parameterValue = instanceRepository.PullInstance(lua);
                    else
                        parameterValue = TypeTools.PullType(lua);

                    parameterValues[index] = parameterValue;

                    if (parameterValue is GenericType genericType)
                    {
                        genericArguments ??= new GenericType[upValueCount - parameterValues.Length];

                        genericArguments[index] = genericType;
                    }
                }

                // Remove the generic types from the parameters
                if (genericArguments != null)
                    parameterValues = parameterValues.Where(p => p is not GenericType).ToArray();
                
                // Pop the table itself which is the first argument passed to __call (and thus lowest on the stack)
                lua.Pop();

                // Handle types that have generic parameters
                if (type.ContainsGenericParameters)
                {
                    var genericTypes = TypeTools.NormalizePossibleGenericTypeArguments(
                        type.GetGenericArguments().Length, 
                        genericArguments,
                        parameterValues.Select(p => p.GetType()).ToList()
                    );
                    
                    type = type.MakeGenericType(genericTypes);
                }

                var parameterTypes = parameterValues.Select(p => p.GetType()).ToList();
                var constructor = type.GetAppropriateConstructor(ref parameterTypes);

                if (constructor == null)
                {
                    var signatures = type.GetConstructorSignatures();
                    var types = string.Join(", ", parameterTypes);
                    
                    throw new Exception($"Incorrect parameters passed to {type.Namespace}.{type.Name} Constructor! {parameterValues.Length} parameters were passed (of types {types}), but only the following overloads exist: \n{signatures}");
                }

                try
                {
                    parameterValues = TypeTools.NormalizeParameters(parameterValues, constructor.GetParameters());
                    
                    type.WarnIfObsolete(lua);
                    constructor.WarnIfObsolete(lua);
                    
                    var instance = constructor.Invoke(parameterValues);
                    
                    instanceRepository.PushInstance(lua, instance);

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
            lua.SetMetaTable(-2); // Pop type meta table
        }

        /// <summary>
        /// Assumes the type table is on top of the stack.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        private void SetManagedProperty(PropertyInfo property, Type type)
        {
            instanceRepository.PushTypeMetatable(lua, type, TypeMetaSubTables.Properties); // Instance meta table
            lua.PushManagedFunction((lua) =>
            {
                var instance = instanceRepository.PullInstance(lua);
                var instanceProperty = instance.GetType().GetProperty(property.Name);

                type.WarnIfObsolete(lua);
                instanceProperty.WarnIfObsolete(lua);
                var value = instanceProperty.GetValue(instance);

                if (TypeTools.IsLuaType(value))
                    lua.PushType(value);
                else
                    instanceRepository.PushInstance(lua, value);

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
            instanceRepository.PushTypeMetatable(lua, type, TypeMetaSubTables.Fields); // Instance meta table
            lua.PushManagedFunction((lua) =>
            {
                var instance = instanceRepository.PullInstance(lua);
                var instanceField = instance.GetType().GetField(field.Name);

                type.WarnIfObsolete(lua);
                instanceField.WarnIfObsolete(lua);
                var value = instanceField.GetValue(instance);

                if (TypeTools.IsLuaType(value))
                    lua.PushType(value);
                else
                    instanceRepository.PushInstance(lua, value);

                return 1;
            });
            lua.SetField(-2, field.Name); // Type field getter
            lua.Pop(); // Pop the instance meta table
        }
    }
}
