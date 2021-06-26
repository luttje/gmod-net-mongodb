using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GmodMongoDb.Binding.Annotating
{
    /// <summary>
    /// 
    /// </summary>
    public struct PropertyMethodNames
    {
        public string? Getter;
        public string? Setter;
    }

    /// <summary>
    /// Apply this attribute to property methods that should be exposed to Lua.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class LuaPropertyAttribute : Attribute
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyMethodNames>> AvailableProperties = new();

        /// <summary>
        /// The name this property will receive in Lua.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// When applying this attribute to methods that should expose to Lua you can specify a name in this constructor.
        /// </summary>
        /// <param name="name">The name the method should receive in Lua</param>
        public LuaPropertyAttribute(string name = null)
        {
            this.Name = name;
        }


        // TODO: Doesn't support generics yet!
        internal static void RegisterAvailableProperty(Type metaTableType, string propertyName, string? getterName, string? setterName)
        {
            if (!AvailableProperties.ContainsKey(metaTableType))
                AvailableProperties[metaTableType] = new Dictionary<string, PropertyMethodNames>();

            AvailableProperties[metaTableType].Add(propertyName, new PropertyMethodNames
            {
                Getter = getterName,
                Setter = setterName
            });
        }

        internal static PropertyMethodNames? GetAvailableProperty(Type metaTableType, string propertyName)
        {
            if (!AvailableProperties.ContainsKey(metaTableType))
                return null;

            if (!AvailableProperties[metaTableType].ContainsKey(propertyName))
                return null;

            return AvailableProperties[metaTableType][propertyName];
        }

        internal static string? GetAvailablePropertyGetter(Type metaTableType, string propertyName)
        {
            return GetAvailableProperty(metaTableType, propertyName)?.Getter;
        }

        internal static string? GetAvailablePropertySetter(Type metaTableType, string propertyName)
        {
            return GetAvailableProperty(metaTableType, propertyName)?.Setter;
        }
    }
}
