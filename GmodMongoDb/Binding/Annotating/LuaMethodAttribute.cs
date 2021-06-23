using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using System;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding.Annotating
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class LuaMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsOverloaded { get; set; }

        public LuaMethodAttribute(string name = null)
        {
            this.Name = name;
            this.IsOverloaded = false;
        }

#nullable enable
        /// <summary>
        /// Pushes a provided method to the Lua stack. If a method is overloaded a finder will be returned that will scan the stack to find the appropriate method to call.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="method">The method to push to the Lua stack</param>
        /// <param name="metaTableTypeId">The type id of the object instance's metatable</param>
        /// <param name="forceWithoutFinder">Forces the method to be pushed, never pushing the finder</param>
        public void PushFunction(ILua lua, MethodInfo method, int metaTableTypeId, bool forceWithoutFinder = false)
        {
            var handle = lua.PushManagedFunction((lua) => {
                LuaMetaObjectBinding? instance = null;

                if (!method.IsStatic)
                {
                    // Copy the object so we can pull it into a .NET object below with `BindingHelper.PullManagedObject`
                    lua.Push(1);
                    int callerCopy = lua.ReferenceCreate(); // Will be freed before returning to Lua

                    instance = TypeConverter.PullManagedObject(lua, metaTableTypeId, 1) as LuaMetaObjectBinding;

                    if (instance is not LuaMetaObjectBinding)
                        throw new NullReferenceException("Invalid instance!");

                    instance.Reference = callerCopy;
                }

                try
                {
                    if (this.IsOverloaded && !forceWithoutFinder)
                    {
                        return FindAndExecuteMethod(method.DeclaringType, instance, lua, method.Name);
                    }

                    return ExecuteMethod(instance, lua, method);
                }
                finally
                {
                    if (instance != null)
                        instance.Reference = null;
                }
            });
            ReferenceManager.Add(handle);
        }

        /// <summary>
        /// Simply runs the specified method on the provided instance, picking arguments from the stack.
        /// </summary>
        /// <param name="instance">The object to run the method on</param>
        /// <param name="lua"></param>
        /// <param name="method">The method to execute</param>
        /// <returns></returns>
        private static int ExecuteMethod(LuaMetaObjectBinding? instance, ILua lua, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var newParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                newParameters[i] = TypeConverter.PullType(lua, parameters[i].ParameterType, 1);

                // Get ready to throw away unused references on close (so methods don't have to do this themselves.
                if (newParameters[i] is LuaReference reference)
                    ReferenceManager.Add(reference);
            }

            // Call the method with the given parameters
            var returned = method.Invoke(instance, newParameters);

            if (method.ReturnType == typeof(void))
                return 0;

            Type? returnedType = returned?.GetType();

            // Convert value returned by method to a Lua recognized type (or metatable if possible)
            if (returnedType == null || returned is not LuaMetaObjectBinding binding)
                return TypeConverter.PushType(lua, returnedType, returned);
            else
            {
                TypeConverter.GenerateUserDataFromObject(lua, binding);
                return 1;
            }
        }

        /// <summary>
        /// Finds a method based on the signature formed by the arguments on the Lua stack
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="instance"></param>
        /// <param name="lua"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static int FindAndExecuteMethod(Type instanceType, LuaMetaObjectBinding? instance, ILua lua, string methodName)
        {
            var stack = lua.Top();
            var signature = new Type[stack];

            for (int i = 0; i < stack; i++)
            {
                signature[i] = TypeConverter.LuaTypeToDotNetType(lua.GetType(i + 1));

                if (signature[i] == null)
                    throw new NotImplementedException("Overloaded methods cant be found if one of the passed arguments is null. Type is needed");
            }

            // Try find an appropriate method by the provided parameter signature
            MethodInfo? method = instanceType.GetMethod(methodName, signature);

            if (method == null)
                throw new ArgumentException($"Method `{methodName}` cannot be called with these types: {string.Join<Type>(", ", signature)}");

            if (!method.GetCustomAttributes<LuaMethodAttribute>().Any())
                throw new AccessViolationException($"Method `{methodName}` cannot be called from Lua! Did you forget to mark it with [LuaMethod]?");

            return ExecuteMethod(instance, lua, method);
        }
#nullable disable
    }
}
