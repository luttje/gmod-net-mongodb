using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// A reference to a Lua function which can be called later.
    /// </summary>
    public class LuaFunctionReference : LuaReference
    {
        /// <summary>
        /// Create a reference for the function currently on the given position of the stack (or the top by default)
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos">The stack position of the function to reference</param>
        public LuaFunctionReference(ILua lua, int stackPos = -1)
            :base(lua, stackPos)
        {
            
        }

        ///<inheritdoc/>
        protected override bool IsValid(int stackPos)
        {
            TYPES type = (TYPES) lua.GetType(stackPos);

            if (type != TYPES.FUNCTION)
            {
                throw new ArgumentException($"Invalid type ({lua.GetTypeName(type)}) detected! Should be a function!");
            }

            return base.IsValid(stackPos);
        }

        /// <summary>
        /// Call this Lua function without expecting results
        /// </summary>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        /// <remarks>Do not call this method from an async method. Instead use <see cref="LuaFunctionReference.CallFromAsync(object[])"/>.</remarks>
        public void Call(params object[] arguments)
        {
            int stack = 0;
            Push();

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    stack += BindingHelper.PushType(lua, argument.GetType(), argument);
                }
            }

            lua.MCall(stack, 0);
        }

        /// <summary>
        /// Call this Lua function expecting multiple results
        /// </summary>
        /// <param name="numResults">The amount of results we expect from the Lua function</param>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        /// <returns>The returned results in an object array</returns>
        /// <remarks>Do not call this method from an async method. Instead use <see cref="LuaFunctionReference.CallFromAsync(object[])"/>.</remarks>
        public object[] Call(int numResults = 0, params object[] arguments)
        {
            int stack = 0;
            Push();

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    stack += BindingHelper.PushType(lua, argument.GetType(), argument);
                }
            }

            lua.MCall(stack, numResults);

            throw new NotImplementedException("TODO: Return data on Lua stack");

            //return new object[] { }; // TODO: Return data on stack
        }

        /// <summary>
        /// Call this Lua function expecting a scalar result
        /// </summary>
        /// <typeparam name="T">The type we expect of the result</typeparam>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        /// <returns>The returned result as the provided type</returns>
        /// <remarks>Do not call this method from an async method. Instead use <see cref="LuaFunctionReference.CallFromAsync(object[])"/>.</remarks>
        public T Call<T>(params object[] arguments)
        {
            int stack = 0;
            Push();

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    stack += BindingHelper.PushType(lua, argument.GetType(), argument);
                }
            }

            lua.MCall(stack, 1);

            return BindingHelper.PullType<T>(lua, -1);
        }

        /// <summary>
        /// This queues the function to be called back once Lua is ready to process it on the main thread.
        /// </summary>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        public void CallFromAsync(params object[] arguments)
        {
            LuaTaskScheduler.AddTask(() => Call(arguments));
        }

        /// <summary>
        /// This queues the function to be called back once Lua is ready to process it on the main thread.
        /// </summary>
        /// <param name="numResults">The amount of results we expect from the Lua function</param>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        /// <returns>A task that's called to return the result as the provided type</returns>
        public async Task<object> CallFromAsync(int numResults = 0, params object[] arguments)
        {
            return await LuaTaskScheduler.AddTask<object[]>(() => Call(numResults, arguments));
        }

        /// <summary>
        /// This queues the function to be called back once Lua is ready to process it on the main thread.
        /// </summary>
        /// <typeparam name="T">The type we expect of the result</typeparam>
        /// <param name="arguments">The parameters to pass to the Lua function</param>
        /// <returns>A task that's called to return the result as the provided type</returns>
        public async Task<object> CallFromAsync<T>(params object[] arguments)
        {
            return await LuaTaskScheduler.AddTask<T>(() => Call<T>(arguments));
        }
    }
}
