using System;

namespace JustDummies.GenAny;

/// <summary>
///     The type a generator is being scaffolded for, as the emitted file needs to spell it.
/// </summary>
/// <remarks>
///     <see cref="Name" /> is the name as C# reads it from inside <see cref="Namespace" /> — <c>Order</c>, or
///     <c>Order.Line</c> for a nested type. That single spelling serves every place the file names the target:
///     the header, the <c>IAny&lt;T&gt;</c> it implements, the <c>&lt;see cref&gt;</c> in its documentation, and
///     the constructor call in <c>Generate()</c>.
///     <para>
///         <see cref="Namespace" /> is where the emitted file goes, which <c>--namespace</c> can move;
///         <see cref="FullName" /> is where the type is declared, which nothing moves. A reader told what was
///         analysed is told the second.
///     </para>
/// </remarks>
public sealed class TargetType {

    /// <summary>
    ///     Declares the target of a scaffold.
    /// </summary>
    /// <param name="name">The type name as written from inside its own namespace.</param>
    /// <param name="namespace">The namespace the emitted file is in, or null for the global namespace.</param>
    /// <param name="style">How that namespace is declared in the emitted file.</param>
    /// <param name="fullName">
    ///     The type's full name where it is declared; when null, <paramref name="name" /> read inside
    ///     <paramref name="namespace" />, which is what it is unless the file was moved to another namespace.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is blank.</exception>
    public TargetType(string name, string? @namespace, NamespaceStyle style, string? fullName = null) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }
        if (name.Trim().Length == 0) { throw new ArgumentException("A target type has a name.", nameof(name)); }

        Name      = name;
        Namespace = @namespace;
        Style     = style;
        FullName  = fullName ?? (@namespace is null ? name : @namespace + "." + name);
    }

    /// <summary>The type name as written from inside its own namespace.</summary>
    public string Name { get; }

    /// <summary>
    ///     The namespace the emitted file is in, or null for the global namespace — the type's own, unless
    ///     <c>--namespace</c> moved the file.
    /// </summary>
    public string? Namespace { get; }

    /// <summary>How that namespace is declared in the emitted file.</summary>
    public NamespaceStyle Style { get; }

    /// <summary>The type's full name where it is declared — <c>Shop.Domain.Order</c>, whatever file it lands in.</summary>
    public string FullName { get; }

}
