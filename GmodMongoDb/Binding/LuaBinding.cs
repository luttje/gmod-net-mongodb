using GmodNET.API;
using System;
using System.Reflection;

namespace GmodMongoDb.Binding
{
    public class LuaBinding
    {
        protected Type BindingType;

        public LuaBinding(Type bindingType)
        {
            BindingType = bindingType;
        }

        public virtual ConstructorInfo PreConstructorInvoke(ILua lua, ConstructorInfo constructor, ref object[] parameters)
            => constructor;

        public virtual MethodBase PreMethodInvoke(ILua lua, object? instance, MethodBase method, ref object[] parameters)
            => method;
        public virtual object? PostMethodInvoke(ILua lua, object instance, MethodBase method, object returned)
            => returned;
        public virtual object? PostConstructorInvoke(ILua lua, ConstructorInfo constructor, object instance)
            => instance;

        internal Type GetBindingType()
            => BindingType;
    }
}