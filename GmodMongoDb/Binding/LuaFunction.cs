using GmodNET.API;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Represents a Managed or Lua function that can be called from Lua or C#.
    /// </summary>
    internal class LuaFunction : IDisposable
    {
        /// <summary>
        /// Keeps a reference to the Lua environment this function is bound to.
        /// </summary>
        private readonly ILua lua;

        /// <summary>
        /// A reference to the function in Lua.
        /// </summary>
        private readonly int reference;

        /// <summary>
        /// Creates a reference to the Lua function that is currently on the top of the stack.
        /// </summary>
        /// <param name="lua"></param>
        public LuaFunction(ILua lua)
        {
            this.lua = lua;
            this.reference = lua.ReferenceCreate();
        }

        /// <summary>
        /// Frees the Lua function reference from memory
        /// </summary>
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
        public void Push(ILua lua)
        {
            lua.ReferencePush(reference);
        }

        /// <summary>
        /// Call the Lua function from C#
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public object InvokeInLua(object[] args)
        {
            Push(lua);

            for (int i = 0; i < args.Length; i++)
            {
                lua.PushType(args[i]);
            }

            lua.MCall(args.Length, 1);

            return TypeTools.PullType(lua);
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

        /// <summary>
        /// Returns a callable delegate (like a Func, Action or Lamda Function expression) that will call this Lua function.
        /// This way a Lua function (of unknown signature) can be used for any delegate in C#.
        /// </summary>
        /// <remarks>
        /// Note that this can not be used to provide an expression to LINQ (and places that use LINQ). This is because a
        /// call to a method (InvokeInLua) can not be converted to an SQL (or other) expression.
        /// </remarks>
        /// <param name="expectedType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        internal object CastTo(Type expectedType)
        {
            if (!GetCastsTo(expectedType))
                throw new InvalidCastException($"Cannot cast LuaFunction to {expectedType}");

            if (expectedType == typeof(LuaFunction))
                return this;

            Type delegateType = expectedType;

            if (typeof(Expression<>).IsAssignableFrom(expectedType.GetGenericTypeDefinition()))
                delegateType = expectedType.GetGenericArguments()[0];

            var delegateInvokeMethod = delegateType.GetMethod("Invoke");
            Type[] parameterTypes = delegateInvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            Type returnType = delegateInvokeMethod.ReturnType;

            // Prepares a variable amount of args, based on the number of parameters in the delegate
            var allArguments = new ParameterExpression[parameterTypes.Length];

            for (int i = 0; i < parameterTypes.Length; i++)
                allArguments[i] = Expression.Parameter(parameterTypes[i], $"arg{i}");

            var allArgumentsAsObject = allArguments.Select(a => Expression.Convert(a, typeof(object))).ToArray();

            var invokeInLuaCall = Expression.Call(
                Expression.Constant(this),
                typeof(LuaFunction).GetMethod(nameof(InvokeInLua)),
                Expression.NewArrayInit(typeof(object), allArgumentsAsObject)
            );

            var convertedResult = Expression.Convert(invokeInLuaCall, returnType);

            var block = Expression.Block(convertedResult);

            // If a delegate is expected, the lambda expression will be compiled and returned as a delegate.
            if (expectedType.IsSubclassOf(typeof(Delegate)))
            {
                var lambda = Expression.Lambda(delegateType, block, allArguments);
                var compiled = lambda.Compile();
                return compiled;
            }

            // If an expression is expected, the lambda expression will be returned.
            var lambdaExpression = Expression.Lambda(block, allArguments);
            return lambdaExpression;
        }
    }
}
