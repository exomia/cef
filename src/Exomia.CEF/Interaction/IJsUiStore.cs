using System.Collections.Generic;

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     Interface for js user interface store.
    /// </summary>
    public interface IJsUiStore
    {
        /// <summary>
        ///     Initial state.
        /// </summary>
        /// <returns>
        ///     An <see cref="IDictionary{TKey,TValue}"/>
        /// </returns>
        IDictionary<string, object> GetState();
    }
}
