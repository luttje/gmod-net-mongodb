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
                // Copy the object so we can pull it into a .NET object below with `BindingHelper.PullManagedObject`
                lua.Push(1);
                int callerCopy = lua.ReferenceCreate(); // Will be freed before returning to Lua

                LuaMetaObjectBinding? instance = BindingHelper.PullManagedObject(lua, metaTableTypeId, 1) as LuaMetaObjectBinding;

                if (instance == null)
                    throw new NullReferenceException("Invalid instance!");

                instance.Reference = callerCopy;

                if (this.IsOverloaded && !forceWithoutFinder)
                {
                    return RunFindFunction(instance, lua, method.Name);
                }

                return RunFunction(instance, lua, method);
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
        protected static int RunFunction(LuaMetaObjectBinding instance, ILua lua, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var newParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                // Commented because:
                // This would break when calling a method like Example(Some=1, Other="asd")
                // like this: Example(nil, "okaok")
                //if (parameters[i].IsOptional && (TYPES)lua.GetType(1) == TYPES.NIL)
                //    break;
                
                newParameters[i] = BindingHelper.PullType(lua, parameters[i].ParameterType, 1);

                if (newParameters[i] is LuaReference reference)
                    ReferenceManager.Add(reference);
            }

            // Call the method with the given parameters
            var returned = method.Invoke(instance, newParameters);

            if (method.ReturnType == typeof(void))
            {
                instance.Reference = null;

                return 0;
            }

            // Convert value returned by method to a Lua recognized type (or metatable if possible)
            bool handledReturnValue = false;
            int stack = 0;
            var returnAttributes = method.ReturnTypeCustomAttributes.GetCustomAttributes(true);

            // Let [LuaValueTransformer] help us if possible
            foreach (var returnAttribute in returnAttributes)
            {
                if (returnAttribute is LuaValueTransformerAttribute)
                {
                    var transformerAttribute = returnAttribute as LuaValueTransformerAttribute;
                    stack = lua.ApplyTransformerConvert(transformerAttribute.Transformer, returned);
                    handledReturnValue = true;
                }
            }

            if (!handledReturnValue)
            {
                Type returnedType = returned.GetType();

                if (returned is LuaMetaObjectBinding binding)
                {
                    BindingHelper.GenerateUserDataFromObject(lua, binding);

                    stack = 1;
                }
                else
                {
                    // Simply return the raw type
                    stack = BindingHelper.PushType(lua, returnedType, returned);
                }
            }

            instance.Reference = null;

            return stack;
        }

        protected static int RunFindFunction(LuaMetaObjectBinding instance, ILua lua, string methodName)
        {
            var stack = lua.Top();
            var signature = new Type[stack];
            Type instanceType = instance.GetType();

            for (int i = 0; i < stack; i++)
            {
                signature[i] = BindingHelper.LuaTypeToDotNetType(lua.GetType(i + 1));
            }

            // Try find an appropriate method by the provided signature
            MethodInfo? method = instanceType.GetMethod(methodName, signature);

            if (method == null)
                throw new ArgumentException($"Method `{methodName}` cannot be called with these types: {string.Join<Type>(", ", signature)}");

            if (method.GetCustomAttributes<LuaMethodAttribute>().Count() == 0)
                throw new AccessViolationException($"Method `{methodName}` cannot be called from Lua! Did you forget to mark it with [LuaMethod]?");

            return RunFunction(instance, lua, method);
        }
#nullable disable
    }
}
