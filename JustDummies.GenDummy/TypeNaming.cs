using System;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenDummy;

/// <summary>
///     The one place an emitted generator gets its name.
/// </summary>
/// <remarks>
///     Routing every caller through a single function is what keeps the naming options of §16 cheap: v1.1 changes
///     this function and binds an option to it, rather than sweeping every site that knew the prefix (specification
///     §11.3).
/// </remarks>
public static class TypeNaming {

    /// <summary>
    ///     The name of the generator emitted for <paramref name="type" /> — <c>DummyOrder</c> for <c>Order</c>.
    /// </summary>
    /// <remarks>
    ///     A nested type is named after itself alone: <c>Order.Line</c> emits a top-level <c>DummyLine</c> in the
    ///     containing namespace (specification §3.2).
    /// </remarks>
    /// <param name="type">The type a generator is being emitted for.</param>
    /// <param name="options">How that generator is to be named.</param>
    /// <returns>The generator's type name, with no namespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> or <paramref name="options" /> is null.</exception>
    public static string GeneratorNameFor(ITypeSymbol type, NamingOptions options) {
        if (type is null) { throw new ArgumentNullException(nameof(type)); }
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        return options.Pattern.Replace(NamingOptions.TypePlaceholder, type.Name);
    }

}
