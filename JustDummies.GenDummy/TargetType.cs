using System;

namespace JustDummies.GenDummy;

/// <summary>
///     The type a generator is being scaffolded for, as the emitted file needs to spell it.
/// </summary>
/// <remarks>
///     <see cref="Name" /> is the name as C# reads it from inside <see cref="Namespace" /> — <c>Order</c>, or
///     <c>Order.Line</c> for a nested type. That single spelling serves every place the file names the target:
///     the header, the <c>IDummy&lt;T&gt;</c> it implements, the <c>&lt;see cref&gt;</c> in its documentation, and
///     the constructor call in <c>Generate()</c>.
/// </remarks>
public sealed class TargetType {

    /// <summary>
    ///     Declares the target of a scaffold.
    /// </summary>
    /// <param name="name">The type name as written from inside its own namespace.</param>
    /// <param name="namespace">The namespace it is declared in, or null for the global namespace.</param>
    /// <param name="style">How that namespace is declared in the type's own file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is blank.</exception>
    public TargetType(string name, string? @namespace, NamespaceStyle style) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }
        if (name.Trim().Length == 0) { throw new ArgumentException("A target type has a name.", nameof(name)); }

        Name      = name;
        Namespace = @namespace;
        Style     = style;
    }

    /// <summary>The type name as written from inside its own namespace.</summary>
    public string Name { get; }

    /// <summary>The namespace the type is declared in, or null for the global namespace.</summary>
    public string? Namespace { get; }

    /// <summary>How that namespace is declared in the type's own file.</summary>
    public NamespaceStyle Style { get; }

}
