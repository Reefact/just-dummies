using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     The base table of §5.2: a parameter's type in, an expression of type <c>IAny&lt;T&gt;</c> out — or
///     nothing, which §5.5 turns into a TODO the developer's own build reports.
/// </summary>
/// <remarks>
///     Every row is subject to ADR-0059: the member is looked up in the developer's compilation before it is
///     written down. A project on the <c>netstandard2.0</c> asset resolves no <c>Any.DateOnly()</c>, and the
///     parameter comes back unresolved instead of carrying a call that does not compile.
///     <para>
///         The table reads types only. What the constructor's own guards say about a parameter — that it is
///         non-empty, positive, bounded — is §5.3, and is not written yet; a parameter therefore gets the
///         neutral generator for its type and nothing more.
///     </para>
/// </remarks>
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id,
                 Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
internal sealed class GeneratorFor {

    /// <summary>The collection row every read-only and mutable list interface shares.</summary>
    private const string ListOf = "ListOf";

    /// <summary>
    ///     How deep composition follows a type through its element types before giving up (§5.2).
    /// </summary>
    /// <remarks>
    ///     Counted in hops between composites, so <c>IReadOnlyList&lt;Dictionary&lt;string, int[]&gt;&gt;</c>
    ///     is exactly three and resolves. Past that a developer is better served by a TODO they can answer
    ///     than by an expression nobody can read.
    ///     <para>
    ///         A cycle cannot form while the table reads types only — a domain type has no row, so following
    ///         one ends immediately. The guard against it belongs with §5.4, where composing through a
    ///         scaffolded generator or a factory is what could make a type reach itself.
    ///     </para>
    /// </remarks>
    private const int MaximumDepth = 3;

    /// <summary>The façade factory for each type the compiler spells as a keyword or knows specially.</summary>
    private static readonly Dictionary<SpecialType, string> BySpecialType = new() {
        [SpecialType.System_Boolean] = "Boolean",
        [SpecialType.System_SByte]   = "SByte",
        [SpecialType.System_Byte]    = "Byte",
        [SpecialType.System_Int16]   = "Int16",
        [SpecialType.System_UInt16]  = "UInt16",
        [SpecialType.System_Int32]   = "Int32",
        [SpecialType.System_UInt32]  = "UInt32",
        [SpecialType.System_Int64]   = "Int64",
        [SpecialType.System_UInt64]  = "UInt64",
        [SpecialType.System_Single]  = "Single",
        [SpecialType.System_Double]  = "Double",
        [SpecialType.System_Decimal] = "Decimal",
        [SpecialType.System_Char]    = "Char",
        [SpecialType.System_String]  = "String",
        [SpecialType.System_DateTime] = "DateTime"
    };

    /// <summary>The façade factory for each named type the table carries, by metadata name.</summary>
    private static readonly Dictionary<string, string> ByMetadataName = new(StringComparer.Ordinal) {
        ["System.Guid"]           = "Guid",
        ["System.DateTime"]       = "DateTime",
        ["System.DateTimeOffset"] = "DateTimeOffset",
        ["System.TimeSpan"]       = "TimeSpan",
        ["System.DateOnly"]       = "DateOnly",
        ["System.TimeOnly"]       = "TimeOnly",
        ["System.Int128"]         = "Int128",
        ["System.UInt128"]        = "UInt128",
        ["System.Half"]           = "Half",
        ["System.Uri"]            = "Uri"
    };

    /// <summary>
    ///     The collection rows, as element-generator factories keyed by the collection's own metadata name.
    /// </summary>
    /// <remarks>
    ///     The interface rows need no adapter: <c>IAny&lt;out T&gt;</c> is covariant, so the
    ///     <c>IAny&lt;List&lt;T&gt;&gt;</c> that <c>Any.ListOf(…)</c> produces is already an
    ///     <c>IAny&lt;IReadOnlyList&lt;T&gt;&gt;</c> (§14.5). Variance across reference conversions is also
    ///     exactly why the value-type nullable row below cannot do the same.
    /// </remarks>
    private static readonly Dictionary<string, string> ByCollection = new(StringComparer.Ordinal) {
        ["System.Collections.Generic.List`1"]                = ListOf,
        ["System.Collections.Generic.IList`1"]               = ListOf,
        ["System.Collections.Generic.IReadOnlyList`1"]       = ListOf,
        ["System.Collections.Generic.ICollection`1"]         = ListOf,
        ["System.Collections.Generic.IReadOnlyCollection`1"] = ListOf,
        ["System.Collections.Generic.IEnumerable`1"]         = "SequenceOf",
        ["System.Collections.Generic.HashSet`1"]             = "SetOf",
        ["System.Collections.Generic.ISet`1"]                = "SetOf"
    };

    private static readonly string[] Dictionaries = [
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.IDictionary`2",
        "System.Collections.Generic.IReadOnlyDictionary`2"
    ];

    private readonly LibrarySurface library;

    private readonly TypeNames names;

    internal GeneratorFor(LibrarySurface library, TypeNames names) {
        this.library = library;
        this.names   = names;
    }

    /// <summary>
    ///     The expression that draws a <paramref name="type" />, or null when the table has no row for it.
    /// </summary>
    internal string? Resolve(ITypeSymbol type) {
        return Resolve(type, MaximumDepth);
    }

    private string? Resolve(ITypeSymbol type, int remaining) {
        // Counted in hops between composites, so the scalar at the bottom is free: a list of dictionaries of
        // arrays is three, and resolves; a fourth wrapper does not.
        if (remaining < 0) { return null; }

        if (type is IArrayTypeSymbol array) { return Collection("ArrayOf", array.ElementType, remaining); }

        if (type is not INamedTypeSymbol named) { return null; }

        if (named.TypeKind == TypeKind.Enum) { return Enum(named); }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            return NullableValue(named, remaining);
        }

        string? collection = Definition(named) is { } definition && ByCollection.TryGetValue(definition, out string? factory)
                                 ? factory
                                 : null;

        if (collection is not null) { return Collection(collection, named.TypeArguments[0], remaining); }

        if (Definition(named) is { } dictionary && Dictionaries.Contains(dictionary, StringComparer.Ordinal)) {
            return Dictionary(named, remaining);
        }

        return Scalar(named);
    }

    /// <summary>A type the table names directly, with the one constraint its row carries.</summary>
    private string? Scalar(INamedTypeSymbol type) {
        if (!ByMetadataName.TryGetValue(MetadataName(type), out string? factory)
         && !BySpecialType.TryGetValue(type.SpecialType, out factory)) {
            return null;
        }

        ITypeSymbol? generator = library.Returned(factory);

        if (generator is null) { return null; }

        return $"Any.{factory}()" + Refinement(factory, generator);
    }

    /// <summary>
    ///     The constraint a row adds to the neutral draw, when the compilation carries it.
    /// </summary>
    /// <remarks>
    ///     <c>Any.String()</c> unconstrained draws zero to sixteen letters and digits — it can return the empty
    ///     string (§14.5) — and a constructor parameter typed <c>string</c> in a domain type is overwhelmingly
    ///     required non-empty. A default that fails about one call in seventeen is the flakiness the library
    ///     exists to remove, so the row is <c>.NonEmpty()</c>. Same argument for <c>Guid</c>, whose empty value
    ///     is the one most guards reject.
    /// </remarks>
    private static string Refinement(string factory, ITypeSymbol generator) {
        return factory switch {
            "String" or "Guid" when LibrarySurface.Carries(generator, "NonEmpty") => ".NonEmpty()",
            "Uri" when LibrarySurface.Carries(generator, "Web")                   => ".Web()",
            _                                                              => string.Empty
        };
    }

    private string? Enum(INamedTypeSymbol type) {
        if (library.Returned("Enum", typeArguments: 1) is null) { return null; }

        return $"Any.Enum<{names.Of(type)}>()";
    }

    private string? Collection(string factory, ITypeSymbol element, int remaining) {
        if (library.Returned(factory, typeArguments: 1, parameters: 1) is null) { return null; }

        string? item = Resolve(element, remaining - 1);

        return item is null ? null : $"Any.{factory}({item})";
    }

    private string? Dictionary(INamedTypeSymbol type, int remaining) {
        if (library.Returned("DictionaryOf", typeArguments: 2, parameters: 2) is null) { return null; }

        string? keys   = Resolve(type.TypeArguments[0], remaining - 1);
        string? values = Resolve(type.TypeArguments[1], remaining - 1);

        return keys is null || values is null ? null : $"Any.DictionaryOf({keys}, {values})";
    }

    /// <summary>
    ///     A nullable value type, which needs the one explicit hop the table carries.
    /// </summary>
    /// <remarks>
    ///     Variance in C# applies across reference conversions only. <c>IAny&lt;string&gt;</c> is an
    ///     <c>IAny&lt;string?&gt;</c> and needs nothing; <c>IAny&lt;int&gt;</c> is <b>not</b> an
    ///     <c>IAny&lt;int?&gt;</c>, so the conversion has to be written. Never <c>.OrNull()</c>: the emitted
    ///     generator does not draw null (ADR-0064).
    /// </remarks>
    private string? NullableValue(INamedTypeSymbol type, int remaining) {
        if (!library.CarriesAs()) { return null; }

        ITypeSymbol underlying = type.TypeArguments[0];
        string?     inner      = Resolve(underlying, remaining);

        return inner is null ? null : $"{inner}.As(value => ({names.Of(type)})value)";
    }

    private static string? Definition(INamedTypeSymbol type) {
        return type.IsGenericType ? MetadataName(type.OriginalDefinition) : null;
    }

    private static string MetadataName(INamedTypeSymbol type) {
        string @namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string name       = type.MetadataName;

        return @namespace.Length == 0 ? name : @namespace + "." + name;
    }

}
