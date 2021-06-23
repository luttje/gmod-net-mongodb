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
    /// <remarks>
    /// If you'd like to create your own transformer you should inherit <see cref="LuaValueTransformer{TTarget}"/>
    /// </remarks>
    /// <example>
    /// Showing how to get all transformers:
    /// <code><![CDATA[
    /// var transformerBaseType = typeof(BaseLuaValueTransformer);
    /// var types = AppDomain.CurrentDomain.GetAssemblies()
    ///     .SelectMany(a => a.GetTypes())
    ///     .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(transformerBaseType));
    /// ]]></code>
    /// </example>
    public abstract class BaseLuaValueTransformer
    {}
}
