using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding
{
    public class DynamicWrapper : IDisposable
    {
        private ILua lua;
        private string? baseName;

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
        /// Gets or creates the namespace table (and all parent tables) for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="namespace"></param>
        private void GetNamespaceTable(string @namespace)
        {
            var parts = @namespace.Split('.');

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
        }

        /// <summary>
        /// Registers a type in sub-tables for each namespace part (seperated by dots)
        /// </summary>
        /// <param name="type"></param>
        public void RegisterType(Type type)
        {
            var @namespace = type.Namespace;

            if (baseName != null)
            {
                @namespace = @namespace?.Substring(baseName.Length).TrimStart('.');
            }

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.GetField(-1, "MongoDB"); // MongoDB table
            lua.Remove(-2); // Pop the global table

            GetNamespaceTable(@namespace); // Namespace table

            lua.CreateTable(); // Type table

            // Register all static methods
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                lua.PushManagedFunction((lua) =>
                {
                    var upValueCount = lua.Top();
                    var parameters = new object[upValueCount];

                    for (int i = 1; i <= upValueCount; i++)
                    {
                        var index = upValueCount - i;
                        parameters[index] = TypeTools.PullType(lua);
                        Console.WriteLine($"Upvalue {index}: {parameters[index]}");
                    }

                    try
                    {
                        var result = method.Invoke(null, parameters);

                        if (result != null)
                        {
                            TypeTools.PushType(lua, result.GetType(), result);
                            return 1;
                        }
                    }
                    catch (TargetParameterCountException e)
                    {
                        throw new Exception($"Incorrect parameters passed to {type.Namespace}.{type.Name}.{method.Name}! {parameters.Length} parameters were passed, but {method.GetParameters().Length} were expected.", e);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"An error occurred while calling {type.Namespace}.{type.Name}.{method.Name}!", e);
                    }
                    
                    return 0;
                });
                lua.SetField(-2, method.Name); // Type method
            }

            lua.SetField(-2, type.Name); // Type table

            lua.Pop(); // Pop the namespace table

            lua.Pop(); // Pop the MongoDB table
        }

        public void Dispose()
        {
            if (baseName == null)
                return;
            
            // Set MongoDB to nil to release all references to the types.
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, baseName);
            lua.Pop();
        }
    }
}
