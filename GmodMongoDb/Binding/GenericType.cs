using System;

namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Represents a generic type. Can be pushed as an instance to Lua, so that it can later be used to build a generic type or method.
    /// </summary>
    internal struct GenericType
    {
        /// <summary>
        /// The type this generic type represents.
        /// </summary>
        public Type Type { get; private set; }

        /// <param name="type"></param>
        public GenericType(Type type)
        {
            Type = type;
        }
    }
}