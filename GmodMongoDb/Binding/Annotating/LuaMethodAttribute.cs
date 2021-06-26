using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using System;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding.Annotating
{
    /// <summary>
    /// Apply this attribute to methods that should be exposed to Lua.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class LuaMethodAttribute : LuaBindingAttribute
    {
        /// <summary>
        /// The name this method will receive in Lua.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates if multiple methods exist with the same name as the one this attribute is applied to.
        /// </summary>
        private bool IsOverloaded { get; set; }

        /// <summary>
        /// Whether the method marked with this attribute is the constructor of the given class.
        /// This means the static functions table will get a __call metamethod that will call this method.
        /// </summary>
        /// <example>
        /// That allows you to do this in Lua:
        /// <code language="Lua"><![CDATA[
        /// local exampleObject = Example()
        /// ]]></code>
        /// </example>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// When applying this attribute to methods that should expose to Lua you can specify a name in this constructor.
        /// </summary>
        /// <param name="name">The name the method should receive in Lua</param>
        public LuaMethodAttribute(string name = null)
        {
            this.Name = name;
            this.IsOverloaded = false;
        }

        private int CallSimpleFunction(ILua lua, MethodBase method, int? metaTableTypeId = null, bool forceWithoutFinder = false)
        {
            object? instance = null;

            if (!method.IsStatic && metaTableTypeId != null)
            {
                // Copy the object so we can pull it into a .NET object below with `BindingHelper.PullManagedObject`
                lua.Push(1);
                int callerCopy = lua.ReferenceCreate(); // Will be freed before returning to Lua

                instance = TypeTools.PullManagedObject(lua, (int)metaTableTypeId, 1);

                if (instance is LuaMetaObjectBinding binding)
                    binding.Reference = callerCopy;
            }

            if (method.IsStatic)
                lua.Remove(1); // Remove the static table reference, we don't need it in this static method.

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
                if (instance is LuaMetaObjectBinding binding && binding != null)
                    binding.Reference = null;
            }
        }

        /// <summary>
        /// Pushes a dummy function which will accept only a table with types. That function will return the real function.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="method"></param>
        /// <param name="metaTableTypeId"></param>
        /// <param name="forceWithoutFinder"></param>
        /// <returns></returns>
        private int CallGenericFunction(ILua lua, MethodBase method, int? metaTableTypeId = null, bool forceWithoutFinder = false)
        {
            var methodInfo = method as MethodInfo;
            var genericArguments = methodInfo.GetGenericArguments();
            var genericTypes = new Type[genericArguments.Length];

            // Pull the instance from the stack
            var instance = TypeTools.PullManagedObject(lua, (int)metaTableTypeId, 1);

            // Pull the generic table from our stack
            var genericsTable = new LuaTableReference(lua, 1);

            genericsTable.ForEach((i, genericType, luaType) =>
            {
                var index = Convert.ToInt32((double)i - 1);

                if (TypeTools.LuaTypeToDotNetType(luaType) != typeof(Type))
                    throw new ArgumentException($"Error! Generic arguments at index {(double)i} needs to be a Type.");

                genericTypes[index] = (Type) genericType;
            });
            genericsTable.Free();

            methodInfo = methodInfo.MakeGenericMethod(genericTypes);

            // Return the real function result
            var handle = lua.PushManagedFunction(lua =>
            {
                // Move the instance back to the front
                TypeTools.PushReference(lua, instance, (int) metaTableTypeId);
                lua.Insert(1);

                return CallSimpleFunction(lua, methodInfo, metaTableTypeId, forceWithoutFinder);
            });
            //ReferenceManager.Add(handle);

            return 1;
        }

#nullable enable
        /// <summary>
        /// Pushes a function to the Lua stack. Lua can call the provided method and this function will automatically convert the arguments. If a method is overloaded a finder will be activated to inspect the stack and find the appropriate method to call.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="method">The method to push to the Lua stack</param>
        /// <param name="metaTableTypeId">The type id of the object instance's metatable or null if static</param>
        /// <param name="forceWithoutFinder">Forces the method to be pushed, never pushing the finder</param>
        public void PushFunction(ILua lua, MethodBase method, int? metaTableTypeId = null, bool forceWithoutFinder = false)
        {
            // Check if the method is overloaded and mark it as such
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (method.DeclaringType.GetMember(method.Name, MemberTypes.Method, BindingFlags.Default).Length > 1)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                this.IsOverloaded = true;

            var handle = lua.PushManagedFunction((lua) => {
                if (method.ContainsGenericParameters)
                {
                    return CallGenericFunction(lua, method, metaTableTypeId, forceWithoutFinder);
                }
                else
                {
                    return CallSimpleFunction(lua, method, metaTableTypeId, forceWithoutFinder);
                }
            });
            //ReferenceManager.Add(handle);
        }

        /// <summary>
        /// Simply runs the specified method on the provided instance, picking arguments from the stack.
        /// </summary>
        /// <param name="instance">The object to run the method on</param>
        /// <param name="lua"></param>
        /// <param name="method">The method to execute</param>
        /// <returns></returns>
        private int ExecuteMethod(object? instance, ILua lua, MethodBase method)
        {
            int offset = 0;
            var parameters = method.GetParameters();
            var newParameters = new object[parameters.Length];

            if (method.IsStatic)
            {
                newParameters[0] = lua;
                offset = 1;
            }

            if(method is ConstructorInfo)
            {
                lua.Remove(1); // Remove the userdata reference from the bottom of the stack
            }

            for (int i = offset; i < parameters.Length; i++)
            {
                var argumentType = (TYPES)lua.GetType(1);

                if (parameters[i].IsOptional && argumentType == TYPES.NIL)
                    continue;

                newParameters[i] = TypeTools.PullType(lua, parameters[i].ParameterType, 1);

                // Get ready to throw away unused references on close (so methods don't have to do this themselves.
                if (newParameters[i] is LuaReference reference)
                    ReferenceManager.Add(reference);
            }

            if (method is MethodInfo methodInfo)
            {
                if (BoundType != null)
                    method = BoundType.PreMethodInvoke(lua, instance, method, ref newParameters);

                // Call the method with the given parameters
                var returned = methodInfo.Invoke(instance, newParameters);

                if (BoundType != null)
                    returned = BoundType.PostMethodInvoke(lua, instance, method, returned);

                if (methodInfo.ReturnType == typeof(void))
                    return 0;

                Type? returnedType = returned?.GetType();

                // Convert value returned by method to a Lua recognized type (or metatable if possible)
                if (returnedType == null || returned is not LuaMetaObjectBinding binding)
                    return TypeTools.PushType(lua, returnedType, returned);
                else
                {
                    TypeTools.GenerateUserDataFromObject(lua, binding);
                    return 1;
                }
            }

            var constructor = method as ConstructorInfo;

            if (constructor == null)
                throw new NullReferenceException("Method is neither constructor nor MethodInfo!? Not supported.");

            if (BoundType != null)
                constructor = BoundType.PreConstructorInvoke(lua, constructor, ref newParameters);

            var newInstance = constructor.Invoke(newParameters);

            if (BoundType != null)
                newInstance = BoundType.PostConstructorInvoke(lua, constructor, newInstance);

            TypeTools.GenerateUserDataFromObject(lua, newInstance);

            return 1;
        }

        /// <summary>
        /// Finds a method based on the signature formed by the arguments on the Lua stack
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="instance"></param>
        /// <param name="lua"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private int FindAndExecuteMethod(Type instanceType, object? instance, ILua lua, string methodName)
        {
            var stack = lua.Top();
            var signature = new Type[stack];

            for (int i = 0; i < stack; i++)
            {
                signature[i] = TypeTools.LuaTypeToDotNetType(lua.GetType(i + 1));

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
