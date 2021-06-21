using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    internal struct LuaScheduledTask
    {
        public Func<object> Callback;
        public Type ReturnType;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    /// <summary>
    /// Helps schedule tasks to be executed safely from asynchronous methods.
    /// </summary>
    public static class LuaTaskScheduler
    {
        private const string HOOK_ID_TICK = "GmodMongoDb.Tick.ProcessQueue";
        private static Queue<LuaScheduledTask> tasks;

        /// <summary>
        /// Have an action be called from the Lua context safely and expect nothing to be returned.
        /// </summary>
        /// <param name="action">The action to call in the Lua context</param>
        /// <returns>A task that yields no result</returns>
        public static Task AddTask(Action action) => AddTask<object>(() => {
            action();

            return null;
        });

        /// <summary>
        /// Have a function be called from the Lua context safely and expect the given type to be returned.
        /// </summary>
        /// <typeparam name="T">The type you expect to be returned</typeparam>
        /// <param name="function">The function to be called in the Lua context</param>
        /// <returns>A task that yields a result of the given type</returns>
        public static Task<object> AddTask<T>(Func<T> function)
        {
            if (tasks == null)
                tasks = new Queue<LuaScheduledTask>();

            var scheduledTask = new LuaScheduledTask
            {
                TaskCompletionSource = new TaskCompletionSource<object>(),
                ReturnType = typeof(T),
                Callback = () => function()
            };
            tasks.Enqueue(scheduledTask);

            return scheduledTask.TaskCompletionSource.Task;
        }

        /// <summary>
        /// Check if there's tasks queued and call them immediately.
        /// </summary>
        /// <param name="lua"></param>
        /// <returns>Always 0 to inform Lua that nothing was returned from this hook</returns>
        public static int ProcessQueue(ILua lua)
        {
            if (tasks == null)
                return 0;

            while (tasks.Count > 0)
            {
                LuaScheduledTask scheduledTask = tasks.Dequeue();

                // Call the Lua function
                var result = scheduledTask.Callback();

                // Inform awaiters that we've returned
                scheduledTask.TaskCompletionSource.SetResult(result);
            }

            return 0;
        }

        /// <summary>
        /// Link the LuaTaskScheduler with Lua by hooking into Tick.
        /// </summary>
        /// <param name="lua"></param>
        public static void RegisterLuaCallback(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "hook");
            lua.GetField(-1, "Add");
            lua.PushString("Tick");
            lua.PushString(HOOK_ID_TICK);
            lua.PushManagedFunction(ProcessQueue);
            lua.MCall(3, 0);
            lua.Pop(2);
        }

        /// <summary>
        /// Unlink the LuaTaskScheduler from Lua by removing the appropriate Tick hook.
        /// </summary>
        /// <param name="lua"></param>
        public static void UnregisterLuaCallback(ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "hook");
            lua.GetField(-1, "Remove");
            lua.PushString("Tick");
            lua.PushString(HOOK_ID_TICK);
            lua.MCall(2, 0);
            lua.Pop(2);
        }
    }
}
