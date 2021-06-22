using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// A reference to a Lua object, for example a table.
    /// </summary>
    public class LuaReference : IDisposable
    {
        /// <summary>
        /// The Lua environment where the reference lives
        /// </summary>
        protected ILua lua;

        /// <summary>
        /// The pointer to the reference in Lua which can be pushed with `lua.ReferencePush` or freed with `lua.ReferenceFree`
        /// </summary>
        protected int? pointer;

        /// <summary>
        /// Creates a reference to the Lua object at the given stack position and removes it from the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="stackPos">The Lua object's position on the stack</param>
        public LuaReference(ILua lua, int stackPos = -1, bool forceKeepOnStack = false)
        {
            this.lua = lua;

            if(stackPos != -1)
            {
                // Move the reference to the top of the stack
                lua.Push(stackPos);

                if(!forceKeepOnStack)
                    lua.Remove(stackPos);
            }

            if (!IsValid(stackPos))
                return;

            pointer = lua.ReferenceCreate();

            ReferenceManager.Add(this);
        }

        /// <summary>
        /// Called to check if the Lua reference is of the type we expect it to be.
        /// </summary>
        /// <param name="stackPos">The Lua object's position on the stack</param>
        /// <returns></returns>
        protected virtual bool IsValid(int stackPos)
        {
            return true;
        }

        /// <summary>
        /// Free the Lua object reference.
        /// </summary>
        public void Free()
        {
            lua.ReferenceFree((int)pointer);
            pointer = null;
        }

        /// <summary>
        /// Push the Lua object reference to the top of the stack.
        /// </summary>
        public void Push()
        {
            lua.ReferencePush((int)pointer);
        }

        /// <summary>
        /// Called to dispose of the object. Calls <see cref="Free"/>
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if(pointer != null)
                Free();
        }
    }
}
