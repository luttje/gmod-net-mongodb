using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    internal class LuaFunction : IDisposable
    {
        private readonly ILua lua;
        private readonly int reference;

        public LuaFunction(ILua lua)
        {
            this.lua = lua;
            this.reference = lua.ReferenceCreate();
        }

        public void Dispose()
        {
            lua.ReferenceFree(reference);
        }

        /// <summary>
        /// Reads the Lua function at the top of the stack and returns it as a <see cref="LuaFunction"/>.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static LuaFunction Get(ILua lua, int stackPos)
        {
            lua.Push(stackPos); // Push the function to the top of the stack
            var func = new LuaFunction(lua);
            //lua.Pop(); // Pop the function from the stack

            return func;
        }

        /// <summary>
        /// Pushes this Lua function to the top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        internal void Push(ILua lua)
        {
            lua.ReferencePush(reference);
        }

        /// <summary>
        /// Calls the referenced function
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TResult Invoke<TResult>(params object[] arguments)
        {
            const int returnAmount = 1; // TODO: Lua Supports multiple returns, we should too
            
            Push(lua);

            for (int i = 0; i < arguments.Length; i++)
            {
                TypeTools.PushType(lua, arguments[i]);
            }

            lua.MCall(arguments.Length, returnAmount);

            return TypeTools.PullType<TResult>(lua);
        }

        /// <summary>
        /// Gets whether LuaFunction can cast to the specified type.
        /// </summary>
        /// <param name="expectedType"></param>
        /// <returns></returns>
        internal static bool GetCastsTo(Type expectedType)
        {
            if (expectedType == typeof(LuaFunction)
                || typeof(Delegate).IsAssignableFrom(expectedType))
                return true;

            var genericType = expectedType.GetGenericTypeDefinition();

            if (!typeof(Expression<>).IsAssignableFrom(genericType))
                return false;

            var genericArg = expectedType.GetGenericArguments()[0];
            
            return GetCastsTo(genericArg);
        }

        internal object CastTo(Type expectedType)
        {
            if (expectedType == typeof(LuaFunction))
            {
                return this;
            }
            else if (typeof(Expression<>).IsAssignableFrom(expectedType.GetGenericTypeDefinition()) 
                && GetCastsTo(expectedType.GetGenericArguments()[0]))
            {
                var genericArgument = expectedType.GetGenericArguments()[0];
                var casted = CastTo(genericArgument);

                // Doesnt work:
                var expressionType = typeof(Expression<>).MakeGenericType(genericArgument);
                var expression = Activator.CreateInstance(expressionType, casted);
                return expression;
            }
            else if (typeof(Delegate).IsAssignableFrom(expectedType))
            {
                Type[] typeArgs = expectedType.GetGenericArguments();
                Delegate method;

                var genericExpectedType = expectedType.GetGenericTypeDefinition();
                if (typeof(Func<>).IsAssignableFrom(genericExpectedType))
                {
                    method = new Func<dynamic>(() => Invoke<dynamic>());
                }
                else if (typeof(Func<,>).IsAssignableFrom(genericExpectedType))
                {
                    method = new Func<dynamic, dynamic>((arg) => Invoke<dynamic>(arg));
                }
                else if (typeof(Func<,,>).IsAssignableFrom(genericExpectedType))
                {
                    method = new Func<dynamic, dynamic, dynamic>((a, b) => Invoke<dynamic>(a, b));
                }
                else
                {
                    throw new NotImplementedException($"Delegate {expectedType} (nor {genericExpectedType}) not yet implemented");
                }

                var methodInfo = expectedType.GetMethod("Invoke");
                var parameters = methodInfo.GetParameters();
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Create a dynamic method with the same signature as the delegate
                //var dynamicMethod = new DynamicMethod(
                //    "DynamicFindWrapper",
                //    methodInfo.ReturnType,
                //    parameterTypes,
                //    typeof(LuaFunction).Module);
                AssemblyName asmName = new AssemblyName("LuaFunction_DynamicMethod_Assembly");
                AssemblyBuilder demoAssembly = AssemblyBuilder.DefineDynamicAssembly(
                    asmName,
                    AssemblyBuilderAccess.Run
                );

                ModuleBuilder demoModule = demoAssembly.DefineDynamicModule(asmName.Name);
                TypeBuilder demoType = demoModule.DefineType(
                    "LuaFunction_DynamicMethod_Type",
                    TypeAttributes.Public
                );

                // Define a Shared, Public method with standard calling
                // conventions. Do not specify the parameter types or the
                // return type, because type parameters will be used for
                // those types, and the type parameters have not been
                // defined yet.
                MethodBuilder dynamicMethod = demoType.DefineMethod(
                    "LuaFunction_DynamicMethod",
                    MethodAttributes.Public | MethodAttributes.Static
                );
                
                // Defining generic parameters for the method makes it a
                // generic method. By convention, type parameters are
                // single alphabetic characters.
                //
                string[] typeParamNames = { "A", "R" };
                GenericTypeParameterBuilder[] typeParameters =
                    dynamicMethod.DefineGenericParameters(typeParamNames);

                Type[] parms = { typeParameters[0] };
                dynamicMethod.SetParameters(parms);

                // Set the return type for the method. The return type is
                // specified by the second type parameter, U.
                dynamicMethod.SetReturnType(typeParameters[1]);

                // Get an ILGenerator and emit the body of the dynamic method
                var il = dynamicMethod.GetILGenerator();

                // il.Emit(OpCodes.Ldnull); // instance

                // Load the arguments onto the stack
                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i);
                }

                // Call the method
                il.Emit(OpCodes.Call, method.Method);

                // Return from the method
                il.Emit(OpCodes.Ret);

                // Complete the type.
                Type dt = demoType.CreateType();

                // To bind types to a dynamic generic method, you must
                // first call the GetMethod method on the completed type.
                // You can then define an array of types, and bind them
                // to the method.
                MethodInfo m = dt.GetMethod("LuaFunction_DynamicMethod");
                MethodInfo bound = m.MakeGenericMethod(typeArgs);

                Console.WriteLine(string.Join(", ", typeArgs.Select(t => t.FullName)));

                // Display a string representing the bound method.
                Console.WriteLine(bound);

                //Console.WriteLine("Created dynamic method with signature: " + dynamicMethod.ReturnType + " " + dynamicMethod.Name + "(" + string.Join(", ", dynamicMethod.GetParameters().Select(p => p.ParameterType)) + ")");
                Console.WriteLine("Expected signature: " + methodInfo.ReturnType + " " + methodInfo.Name + "(" + string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType)) + ")");
                Console.WriteLine("Bound signature: " + bound.ReturnType + " " + bound.Name + "(" + string.Join(", ", bound.GetParameters().Select(p => p.ParameterType)) + ")");

                //var @delegate = dynamicMethod.CreateDelegate(expectedType);

                //return @delegate;

                return bound.CreateDelegate(expectedType);
            }

            throw new NotImplementedException("Cannot create lua function from " + expectedType.FullName);
        }
    }

    /*
        if (typeof(Func<>).IsAssignableFrom(expectedType))
            return new Func<object>(() => Invoke<object>());
        else if (typeof(Func<,>).IsAssignableFrom(expectedType))
            return new Func<object, object>((a) => Invoke<object>(a));
        else if (typeof(Func<,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object>((a, b) => Invoke<object>(a, b));
        else if (typeof(Func<,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object>((a, b, c) => Invoke<object>(a, b, c));
        else if (typeof(Func<,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object>((a, b, c, d) => Invoke<object>(a, b, c, d));
        else if (typeof(Func<,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object>((a, b, c, d, e) => Invoke<object>(a, b, c, d, e));
        else if (typeof(Func<,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object>((a, b, c, d, e, f) => Invoke<object>(a, b, c, d, e, f));
        else if (typeof(Func<,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g) => Invoke<object>(a, b, c, d, e, f, g));
        else if (typeof(Func<,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h) => Invoke<object>(a, b, c, d, e, f, g, h));
        else if (typeof(Func<,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i) => Invoke<object>(a, b, c, d, e, f, g, h, i));
        else if (typeof(Func<,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j) => Invoke<object>(a, b, c, d, e, f, g, h, i, j));
        else if (typeof(Func<,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k));
        else if (typeof(Func<,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l));
        else if (typeof(Func<,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m));
        else if (typeof(Func<,,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n));
        else if (typeof(Func<,,,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o));
        else if (typeof(Func<,,,,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Func<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p));
        else if (typeof(Action).IsAssignableFrom(expectedType))
            return new Action(() => Invoke<object>());
        else if (typeof(Action<>).IsAssignableFrom(expectedType))
            return new Action<object>((a) => Invoke<object>(a));
        else if (typeof(Action<,>).IsAssignableFrom(expectedType))
            return new Action<object, object>((a, b) => Invoke<object>(a, b));
        else if (typeof(Action<,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object>((a, b, c) => Invoke<object>(a, b, c));
        else if (typeof(Action<,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object>((a, b, c, d) => Invoke<object>(a, b, c, d));
        else if (typeof(Action<,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object>((a, b, c, d, e) => Invoke<object>(a, b, c, d, e));
        else if (typeof(Action<,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object>((a, b, c, d, e, f) => Invoke<object>(a, b, c, d, e, f));
        else if (typeof(Action<,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object>((a, b, c, d, e, f, g) => Invoke<object>(a, b, c, d, e, f, g));
        else if (typeof(Action<,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h) => Invoke<object>(a, b, c, d, e, f, g, h));
        else if (typeof(Action<,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i) => Invoke<object>(a, b, c, d, e, f, g, h, i));
        else if (typeof(Action<,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j) => Invoke<object>(a, b, c, d, e, f, g, h, i, j));
        else if (typeof(Action<,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k));
        else if (typeof(Action<,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l));
        else if (typeof(Action<,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m));
        else if (typeof(Action<,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n));
        else if (typeof(Action<,,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o));
        else if (typeof(Action<,,,,,,,,,,,,,,,>).IsAssignableFrom(expectedType))
            return new Action<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => Invoke<object>(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p));
        else
            throw new NotSupportedException("Cannot create lua function delegate for type " + expectedType.FullName);
     */
}
