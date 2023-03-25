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
        /// Returns a string describing all available method signatures
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static string GetMethodSignatures(this Type type, string methodName)
        {
            var methods = type.GetMethods().Where(m => m.Name == methodName);
            var signatures = new StringBuilder();

            foreach (var validMethod in methods)
            {
                signatures.Append("\t- ");
                signatures.Append(validMethod.ReturnType.Name);
                signatures.Append(" ");
                signatures.Append(validMethod.Name);
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
                        signatures.Append(methodParameters[i].DefaultValue);
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
                        signatures.Append(constructorParameters[i].DefaultValue);
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
            var methods = type.GetMethods().Where(m => m.Name == methodName);
            
            foreach (var method in methods)
            {
                var methodParameters = method.GetParameters();

                // Get the base method (if we override it)
                // We do this because for some reason in MongoDB the overriding implementation signature differs from the base implementation
                // impl: https://github.com/mongodb/mongo-csharp-driver/blob/2d5f467180085228cfca2fb1ca3f53261da44a9c/src/MongoDB.Driver/MongoDatabaseImpl.cs#L343
                // base: https://github.com/mongodb/mongo-csharp-driver/blob/2d5f467180085228cfca2fb1ca3f53261da44a9c/src/MongoDB.Driver/MongoDatabaseBase.cs#L166
                var baseMethod = method.GetBaseDefinition();

                if (baseMethod != method)
                {
                    methodParameters = baseMethod.GetParameters();
                }
                
                if (methodParameters.Length < parameterTypes.Count) 
                {
                    continue;
                }
                
                bool isAppropriate = true;

                for (int i = 0; i < methodParameters.Length; i++)
                {
                    if (i < parameterTypes.Count && methodParameters[i].ParameterType != parameterTypes[i])
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

                bool isAppropriate = true;

                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    if (i < parameterTypes.Count && constructorParameters[i].ParameterType != parameterTypes[i])
                    {
                        isAppropriate = false;
                        break;
                    }
                    else if (i >= parameterTypes.Count && !(constructorParameters[i].IsOptional || constructorParameters[i].HasDefaultValue))
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
    }
}
