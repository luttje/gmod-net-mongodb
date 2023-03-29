namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Types/classes have metatables and each metatable has all these subtables.
    /// </summary>
    public enum TypeMetaSubTables
    {
        /// <summary>
        /// Subtable that contains methods that fetch properties for the type/class.
        /// </summary>
        Properties = 1,

        /// <summary>
        /// Subtable that contains methods to fetch fields for the type/class.
        /// </summary>
        Fields = 2,
    }
}
