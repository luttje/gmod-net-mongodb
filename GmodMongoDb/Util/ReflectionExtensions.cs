using GmodMongoDb.Binding;
using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Util
{
    internal static class ReflectionExtensions
    {
        /// <summary>
        /// Gives a warning if the ObsoleteAttribute is found on the member.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="lua"></param>
        public static void WarnIfObsolete(this MemberInfo member, ILua lua)
        {
            var obsoleteAttribute = member.GetCustomAttribute<ObsoleteAttribute>();

            if (obsoleteAttribute == null)
                return;

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, $"{LuaExtensions.CONSTANT_PREFIX}SUPPRESS_OBSOLETE_WARNINGS");
            if(lua.IsType(-1, TYPES.BOOL))
            {
                var isSuppressed = lua.GetBool(-1);

                if (isSuppressed)
                {
                    lua.Pop(2); // pop bool and global table
                    return;
                }
            }
            lua.Pop(2); // pop nil(or false) and global table

            Console.WriteLine($"Warning: {member.Name} Constructor is marked as obsolete: {obsoleteAttribute.Message}");
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsWithBase(this Type type, string name)
        {
            var ownMethods = type.GetMethods().Where(m => m.Name == name);
            var methods = new List<MethodInfo>();

            // Get the base methods (if we override it)
            // We do this because for some reason in MongoDB the overriding implementation signature differs from the base implementation
            // impl: https://github.com/mongodb/mongo-csharp-driver/blob/2d5f467180085228cfca2fb1ca3f53261da44a9c/src/MongoDB.Driver/MongoDatabaseImpl.cs#L343
            // base: https://github.com/mongodb/mongo-csharp-driver/blob/2d5f467180085228cfca2fb1ca3f53261da44a9c/src/MongoDB.Driver/MongoDatabaseBase.cs#L166
            foreach (var ownMethod in ownMethods)
            {
                var method = ownMethod;
                Type lastDeclaringType;
                do
                {
                    lastDeclaringType = method.DeclaringType;
                    methods.Add(method);
                    method = method.GetBaseDefinition();
                } while (method.DeclaringType != lastDeclaringType);
            }

            return methods.ToArray();
        }
        
        /// <summary>
        /// Returns a string describing all available method signatures
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static string GetMethodSignatures(this Type type, string methodName)
        {
            var methods = type.GetMethodsWithBase(methodName);
            var signatures = new StringBuilder();

            signatures.AppendLine($"Found {methods.Length} method(s) with name {methodName}.");
            signatures.AppendLine($"(Methods marked with * were found in a base class.)");

            foreach (var validMethod in methods)
            {
                signatures.Append("\t- ");
                signatures.Append(validMethod.ReturnType.Name);
                signatures.Append(" ");
                signatures.Append(validMethod.Name);

                if (!validMethod.DeclaringType.Equals(type))
                    signatures.Append("*");

                signatures.Append("(");

                var methodParameters = validMethod.GetParameters();

                for (int i = 0; i < methodParameters.Length; i++)
                {
                    signatures.Append(methodParameters[i].ParameterType.Name);
                    signatures.Append(" ");
                    signatures.Append(methodParameters[i].Name);

                    if (methodParameters[i].HasDefaultValue)
                    {
                        signatures.Append(" = ");
                        signatures.Append(methodParameters[i].DefaultValue ?? "null");
                    }

                    if (i < methodParameters.Length - 1)
                    {
                        signatures.Append(", ");
                    }
                }

                signatures.Append(")\n");
            }

            return signatures.ToString();
        }

        /// <summary>
        /// Returns a string describing all available constructor signatures
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetConstructorSignatures(this Type type)
        {
            var constructors = type.GetConstructors();
            var signatures = new StringBuilder();

            foreach (var constructor in constructors)
            {
                signatures.Append("\t- ");
                signatures.Append(type.Name);
                signatures.Append("(");

                var constructorParameters = constructor.GetParameters();

                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    signatures.Append(constructorParameters[i].ParameterType.Name);
                    signatures.Append(" ");
                    signatures.Append(constructorParameters[i].Name);

                    if (constructorParameters[i].HasDefaultValue)
                    {
                        signatures.Append(" = ");
                        signatures.Append(constructorParameters[i].DefaultValue ?? "null");
                    }
                    
                    if (i < constructorParameters.Length - 1)
                    {
                        signatures.Append(", ");
                    }
                }

                signatures.Append(")\n");
            }

            return signatures.ToString();
        }

        /// <summary>
        /// Gets a method that fits the provided parameter types
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        public static MethodInfo GetAppropriateMethod(this Type type, string methodName, ref List<Type> parameterTypes)
        {
            var methods = type.GetMethodsWithBase(methodName);

            foreach (var method in methods)
            {
                var methodParameters = method.GetParameters();

                if (methodParameters.Length < parameterTypes.Count)
                    continue;

                bool isAppropriate = true;

                for (int i = 0; i < methodParameters.Length; i++)
                {
                    if (i < parameterTypes.Count
                        && !methodParameters[i].ParameterType.IsAssignableFrom(parameterTypes[i])
                        && !(
                            methodParameters[i].ParameterType.IsGenericType
                            && methodParameters[i].ParameterType.GetGenericTypeDefinition().IsSubclassOfRawGeneric(parameterTypes[i])
                        ))
                    {
                        isAppropriate = false;
                        break;
                    }
                    else if (i >= parameterTypes.Count && !(methodParameters[i].IsOptional || methodParameters[i].HasDefaultValue))
                    {
                        isAppropriate = false;
                        break;
                    }
                    else if (i >= parameterTypes.Count)
                    {
                        parameterTypes.Add(Type.Missing.GetType());
                    }
                }

                if (isAppropriate)
                    return method;
            }

            return null;
        }

        /// <summary>
        /// Gets a constructor that fits the provided parameter types
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        public static ConstructorInfo GetAppropriateConstructor(this Type type, ref List<Type> parameterTypes)
        {
            var constructors = type.GetConstructors();

            foreach (var constructor in constructors)
            {
                var constructorParameters = constructor.GetParameters();

                if (constructorParameters.Length < parameterTypes.Count)
                    continue;

                var normalizedParameterTypes = TypeTools.NormalizeParameterTypes(parameterTypes, constructorParameters);

                bool isAppropriate = true;

                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    var normalized = (Type)normalizedParameterTypes[i];

                    if (i < normalizedParameterTypes.Count 
                        && !constructorParameters[i].ParameterType.IsAssignableFrom(normalized)
                        && !(
                            constructorParameters[i].ParameterType.IsGenericType
                            && constructorParameters[i].ParameterType.GetGenericTypeDefinition().IsSubclassOfRawGeneric(normalized)
                        ))
                    {
                        isAppropriate = false;
                        break;
                    }
                    else if (i >= normalizedParameterTypes.Count && !(constructorParameters[i].IsOptional || constructorParameters[i].HasDefaultValue))
                    {
                        isAppropriate = false;
                        break;
                    }
                    else if (i >= parameterTypes.Count)
                    {
                        parameterTypes.Add(Type.Missing.GetType());
                    }
                }

                if (isAppropriate)
                    return constructor;
            }

            return null;
        }

        /// <summary>
        /// Checks a generic type to see if it is assignable to another generic type.
        /// https://stackoverflow.com/a/5461399
        /// </summary>
        /// <param name="thisGenericType"></param>
        /// <param name="otherType"></param>
        /// <returns></returns>
        public static bool IsAssignableToGenericType(this Type thisGenericType, Type otherType)
        {
            var equals = thisGenericType.Equals(otherType);
            var equiv = thisGenericType.IsEquivalentTo(otherType);

            if (thisGenericType.IsAssignableTo(otherType))
                return true;

            var interfaceTypes = thisGenericType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == otherType)
                    return true;
            }

            if (thisGenericType.IsGenericType && thisGenericType.GetGenericTypeDefinition() == otherType)
                return true;

            Type baseType = thisGenericType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, otherType);
        }

        /// <summary>
        /// Checks a generic type to see if it is assignable from another generic type.
        /// </summary>
        /// <param name="thisGenericType"></param>
        /// <param name="otherType"></param>
        /// <returns></returns>
        public static bool IsAssignableFromGenericType(this Type thisGenericType, Type otherType)
        {
            return otherType.IsAssignableToGenericType(thisGenericType);
        }

        /// <summary>
        /// https://stackoverflow.com/a/457708
        /// </summary>
        /// <param name="rawGeneric"></param>
        /// <param name="otherType"></param>
        /// <returns></returns>
        static bool IsSubclassOfRawGeneric(this Type rawGeneric, Type otherType)
        {
            while (otherType != null && otherType != typeof(object))
            {
                var cur = otherType.IsGenericType ? otherType.GetGenericTypeDefinition() : otherType;
                if (rawGeneric == cur)
                {
                    return true;
                }
                otherType = otherType.BaseType;
            }
            return false;
        }
    }
}
