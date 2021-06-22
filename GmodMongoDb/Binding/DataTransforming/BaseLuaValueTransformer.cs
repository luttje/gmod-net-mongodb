using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.DataTransforming
{
    /// <summary>
    /// Nothing should derive from this class. It is only for detecting which classes are transformers, without having to provide a generic type.
    /// </summary>
    /// <example>
    /// Showing how to get all transformers:
    /// <code>
    /// var transformerBaseType = typeof(BaseLuaValueTransformer);
    /// var types = AppDomain.CurrentDomain.GetAssemblies()
    ///     .SelectMany(a => a.GetTypes())
    ///     .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(transformerBaseType));
    /// </code>
    /// </example>
    public abstract class BaseLuaValueTransformer
    {}
}
