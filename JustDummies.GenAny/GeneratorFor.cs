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

    /// <summary>The rows that exist only on the net8.0 asset, so their absence can be reported as such.</summary>
    private static readonly string[] ModernOnly = ["DateOnly", "TimeOnly", "Int128", "UInt128", "Half"];

    private readonly LibrarySurface library;

    private readonly TypeNames names;

    private readonly Compilation compilation;

    private readonly NamingOptions naming;

    internal GeneratorFor(LibrarySurface library, TypeNames names, Compilation compilation, NamingOptions naming) {
        this.library     = library;
        this.names       = names;
        this.compilation = compilation;
        this.naming      = naming;
    }

    /// <summary>Whether a size guard on this type reads against the count family rather than the length one.</summary>
    /// <remarks>
    ///     A collection generator exposes <c>NonEmpty</c>, <c>WithCount</c>, <c>WithMinCount</c> and
    ///     <c>WithMaxCount</c>, and no <c>WithLength</c> at all (§14.3). Reading <c>p.Length &gt; N</c> on a
    ///     <c>T[]</c> against the string family would emit a member ADR-0059 drops <b>silently</b> — a real
    ///     constraint lost without a trace.
    /// </remarks>
    internal static bool SizedByCount(ITypeSymbol type) {
        if (type is IArrayTypeSymbol) { return true; }

        return type is INamedTypeSymbol named
            && Definition(named) is { } definition
            && (ByCollection.ContainsKey(definition) || Dictionaries.Contains(definition, StringComparer.Ordinal));
    }

    /// <summary>
    ///     The generator for <paramref name="type" />, in the three parts a guard can be slotted between.
    /// </summary>
    internal DrawnGenerator Draw(ITypeSymbol type) {
        return Draw(type, MaximumDepth, []);
    }

    /// <summary>
    ///     The complete expression for <paramref name="drawn" />, with <paramref name="guards" /> read into it.
    /// </summary>
    /// <remarks>
    ///     Every constraint is checked against the builder before it is written (ADR-0059): <c>.Positive()</c>
    ///     on a <c>uint</c> parameter does not resolve, and is skipped rather than emitted.
    /// </remarks>
    internal static string Chain(DrawnGenerator drawn, IReadOnlyList<GuardConstraint> guards, out bool dropped) {
        IReadOnlyList<GuardConstraint> kept = GuardReading.Combine([.. drawn.Seeded, .. guards], out dropped);

        string chain = string.Concat(kept.Where(constraint => LibrarySurface.Carries(drawn.Builder,
                                                                                     constraint.Member,
                                                                                     constraint.Arity))
                                         .Select(constraint => constraint.Render()));

        return drawn.Core + chain + drawn.Suffix;
    }

    /// <summary>The complete expression for a type nothing further will constrain — an element, a key.</summary>
    private string? Resolve(ITypeSymbol type, int remaining, IReadOnlyCollection<ITypeSymbol> underway) {
        DrawnGenerator drawn = Draw(type, remaining, underway);

        return drawn.Resolved ? Chain(drawn, [], out _) : null;
    }

    private DrawnGenerator Draw(ITypeSymbol type, int remaining, IReadOnlyCollection<ITypeSymbol> underway) {
        // Counted in hops between composites, so the scalar at the bottom is free: a list of dictionaries of
        // arrays is three, and resolves; a fourth wrapper does not.
        if (remaining < 0) { return DrawnGenerator.Unresolved(); }

        // A type that reaches itself — Email.Create(Email) — would otherwise be followed until the depth bound
        // stopped it, which reads as a coincidence rather than as the rule it is.
        if (underway.Any(seen => SymbolEqualityComparer.Default.Equals(seen, type))) { return DrawnGenerator.Unresolved(); }

        if (type is IArrayTypeSymbol array) { return Collection("ArrayOf", array.ElementType, remaining, underway, type); }

        if (type is not INamedTypeSymbol named) { return DrawnGenerator.Unresolved(); }

        if (named.TypeKind == TypeKind.Enum) { return Enum(named); }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            return NullableValue(named, remaining, underway);
        }

        string? collection = Definition(named) is { } definition && ByCollection.TryGetValue(definition, out string? factory)
                                 ? factory
                                 : null;

        if (collection is not null) { return Collection(collection, named.TypeArguments[0], remaining, underway, named); }

        if (Definition(named) is { } dictionary && Dictionaries.Contains(dictionary, StringComparer.Ordinal)) {
            return Dictionary(named, remaining, underway);
        }

        DrawnGenerator scalar = Scalar(named);

        return scalar.Resolved ? scalar : Composed(named, remaining, underway, scalar.Provenance);
    }

    /// <summary>A type the table names directly, with the one constraint its row carries.</summary>
    private DrawnGenerator Scalar(INamedTypeSymbol type) {
        if (!ByMetadataName.TryGetValue(MetadataName(type), out string? factory)
         && !BySpecialType.TryGetValue(type.SpecialType, out factory)) {
            return DrawnGenerator.Unresolved();
        }

        ITypeSymbol? generator = library.Returned(factory);

        if (generator is null) {
            // The library HAS this generator; this project's asset does not. Saying so turns a dead end into
            // an instruction — retarget, or write it yourself — where "not inferred" would not.
            return DrawnGenerator.Unresolved(ModernOnly.Contains(factory, StringComparer.Ordinal)
                                                 ? Provenance.Unavailable
                                                 : Provenance.None);
        }

        return DrawnGenerator.From($"Any.{factory}()", generator, Refinement(factory, generator));
    }

    /// <summary>
    ///     A type the base table has no row for, drawn through the developer's own code instead (§5.4).
    /// </summary>
    private DrawnGenerator Composed(INamedTypeSymbol type,
                                    int remaining,
                                    IReadOnlyCollection<ITypeSymbol> underway,
                                    Provenance refusal) {
        if (refusal != Provenance.None) { return DrawnGenerator.Unresolved(refusal); }

        INamedTypeSymbol? scaffolded = Composition.ScaffoldedFor(type, compilation, naming);

        if (scaffolded is not null) {
            names.Open(NamespaceOf(scaffolded));

            return DrawnGenerator.From($"new {names.Of(scaffolded)}()", scaffolded, provenance: Provenance.Scaffolded);
        }

        IMethodSymbol? factory = Composition.FactoryFor(type);

        if (factory is null) { return DrawnGenerator.Unresolved(); }

        IParameterSymbol source = factory.Parameters[0];
        DrawnGenerator   inner  = Draw(source.Type, remaining - 1, [.. underway, type]);

        if (!inner.Resolved) { return DrawnGenerator.Unresolved(); }

        // Guard reading is what makes factory composition correct rather than nominally present:
        // OrderReference.Create guards on IsNullOrWhiteSpace, so the chain becomes
        // Any.String().NonEmpty().As(OrderReference.Create) — one measured throwing about one draw in
        // seventeen without it.
        GuardReading                   read       = Guards.Read(factory, compilation);
        IReadOnlyList<GuardConstraint> tightening = read.For(source.Name);
        Provenance                     provenance = Provenance.Factory
                                                  | (tightening.Count > 0 ? Provenance.Guard : Provenance.None)
                                                  | (read.SourceAvailable ? Provenance.None : Provenance.NoSource)
                                                  | (read.Unread(source.Name) ? Provenance.UnreadGuards : Provenance.None);

        return inner.Then($".As({names.Of(type)}.{factory.Name})", provenance, tightening);
    }

    private static string NamespaceOf(INamedTypeSymbol type) {
        return type.ContainingNamespace is { IsGlobalNamespace: false } @namespace ? @namespace.ToDisplayString() : string.Empty;
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
    private static IReadOnlyList<GuardConstraint> Refinement(string factory, ITypeSymbol generator) {
        return factory switch {
            "String" or "Guid" when LibrarySurface.Carries(generator, "NonEmpty") =>
                [new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness)],
            "Uri" when LibrarySurface.Carries(generator, "Web") =>
                [new GuardConstraint("Web", argument: null, Bound.Exact)],
            _ => []
        };
    }

    private DrawnGenerator Enum(INamedTypeSymbol type) {
        ITypeSymbol? generator = library.Returned("Enum", typeArguments: 1);

        return generator is null
                   ? DrawnGenerator.Unresolved()
                   : DrawnGenerator.From($"Any.Enum<{names.Of(type)}>()", generator);
    }

    private DrawnGenerator Collection(string factory,
                                      ITypeSymbol element,
                                      int remaining,
                                      IReadOnlyCollection<ITypeSymbol> underway,
                                      ITypeSymbol self) {
        ITypeSymbol? generator = library.Returned(factory, typeArguments: 1, parameters: 1);

        if (generator is null) { return DrawnGenerator.Unresolved(); }

        string? item = Resolve(element, remaining - 1, [.. underway, self]);

        return item is null
                   ? DrawnGenerator.Unresolved()
                   : DrawnGenerator.From($"Any.{factory}({item})", generator);
    }

    private DrawnGenerator Dictionary(INamedTypeSymbol type, int remaining, IReadOnlyCollection<ITypeSymbol> underway) {
        ITypeSymbol? generator = library.Returned("DictionaryOf", typeArguments: 2, parameters: 2);

        if (generator is null) { return DrawnGenerator.Unresolved(); }

        string? keys   = Resolve(type.TypeArguments[0], remaining - 1, [.. underway, type]);
        string? values = Resolve(type.TypeArguments[1], remaining - 1, [.. underway, type]);

        return keys is null || values is null
                   ? DrawnGenerator.Unresolved()
                   : DrawnGenerator.From($"Any.DictionaryOf({keys}, {values})", generator);
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
    private DrawnGenerator NullableValue(INamedTypeSymbol type, int remaining, IReadOnlyCollection<ITypeSymbol> underway) {
        if (!library.CarriesAs()) { return DrawnGenerator.Unresolved(); }

        DrawnGenerator inner = Draw(type.TypeArguments[0], remaining, [.. underway, type]);

        return inner.Resolved
                   ? inner.Then($".As(value => ({names.Of(type)})value)", Provenance.None)
                   : DrawnGenerator.Unresolved(inner.Provenance);
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
