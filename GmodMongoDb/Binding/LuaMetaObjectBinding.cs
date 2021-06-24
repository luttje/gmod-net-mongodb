using GmodMongoDb.Binding.Annotating;
using GmodNET.API;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// The baseclass from which metatable classes can inherit. These classes specify how the metatable should look in Lua.
    /// Metatable classes can be marked with [LuaMetaTable] to give them an explicit type name. Otherwise their class name will be used.
    /// </summary>
    public abstract class LuaMetaObjectBinding
    {
        /// <summary>
        /// The metatable type id for this object, as returned by `lua.CreateMetaTable`
        /// </summary>
        public int? MetaTableTypeId { get; set; }

        /// <summary>
        /// The pointer to the Lua instance of this object. Only filled when a method on this class is called from Lua.
        /// </summary>
        public int? Reference
        {
            get => reference; 
            
            internal set
            {
                if (value == null && reference != null)
                    lua.ReferenceFree((int)reference);

                reference = value;
            }
        }
        private int? reference;

        /// <summary>
        /// The Lua environment where this object lives
        /// </summary>
        protected ILua lua;

        /// <summary>
        /// Instantiates the .NET Object, informing it of the Lua environment it's part of.
        /// </summary>
        /// <param name="lua">The Lua environment where this object lives</param>
        public LuaMetaObjectBinding(ILua lua)
        {
            this.MetaTableTypeId = null;
            this.Reference = null;
            this.lua = lua;
        }

        [LuaMethod("__index")]
        public virtual object Index(string key)
        {
            string? getterName = LuaPropertyAttribute.GetAvailablePropertyGetter(GetType(), key);

            if (getterName != null)
            {
                var method = GetType().GetMethod(getterName);

                return method.Invoke(method.IsStatic ? null : this, null);
            }

            if (MetaTableTypeId == null)
                return null;

            lua.PushMetaTable((int)MetaTableTypeId);
            lua.GetField(-1, key);

            object result = TypeTools.PullType(lua, -1);
            lua.Pop(1); // pop the metatable

            return result;
        }

        [LuaMethod("__newindex")]
        public virtual void NewIndex(string key, LuaReference valueReference)
        {
            string? setterName = LuaPropertyAttribute.GetAvailablePropertySetter(GetType(), key);

            if (setterName != null)
            {
                var method = GetType().GetMethod(setterName);

                valueReference.Push();
                object value = TypeTools.PullType(lua, method.GetParameters()[0].ParameterType, -1);
                valueReference.Free();

                method.Invoke(method.IsStatic ? null : this, new[] { value });
                return;
            }

            throw new InvalidOperationException($"You can not set the property `{key}` on object `{this}`! The property does not exist.");
        }
    }
}
