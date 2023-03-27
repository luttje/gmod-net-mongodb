using System;

namespace GmodMongoDb.Binding
{
    internal struct GenericType
    {
        public Type Type { get; private set; }

        public GenericType(Type type)
        {
            Type = type;
        }
    }
}