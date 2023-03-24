using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    public class DynamicWrapper
    {
        /// <summary>
        /// Gets or creates the namespace table (and all parent tables) for the given type. Puts it on top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="namespace"></param>
        public static void GetNamespaceTable(ILua lua, string @namespace)
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

        public static void RegisterType(ILua lua, Type type, string? trimNamespace = null)
        {
            var @namespace = type.Namespace;

            if (trimNamespace != null)
            {
                @namespace = @namespace?.Substring(trimNamespace.Length).TrimStart('.');
            }

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB); // Global table
            lua.GetField(-1, "MongoDB"); // MongoDB table
            lua.Remove(-2); // Pop the global table

            GetNamespaceTable(lua, @namespace); // Namespace table

            lua.CreateTable(); // Type table

            // Register all static methods
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                lua.PushManagedFunction((lua) =>
                {
                    Console.WriteLine($"{@namespace}.{type.Name}.{method.Name} was called");

                    var upValueCount = lua.Top();
                    var parameters = new object[upValueCount];

                    for (int i = 1; i <= upValueCount; i++)
                    {
                        var index = upValueCount - i;
                        parameters[index] = TypeTools.PullType(lua);
                        Console.WriteLine($"Upvalue {index}: {parameters[index]}");
                    }

                    LuaExtensions.Print(lua, "Function called!");
                    LuaExtensions.Print(lua, lua.GetStack());

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
    }
}
