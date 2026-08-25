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
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
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

    /// <summary>
    ///     The largest size argument the library will accept, mirrored from <c>SizeGuard</c> (ADR-0076).
    /// </summary>
    /// <remarks>
    ///     A copy, because ADR-0063 keeps the engine from referencing the library to ask. Copies drift, so this
    ///     one is held to the original by a test that pins the engine's boundary and the library's against each
    ///     other at the same number, without either of them naming it.
    /// </remarks>
    private const int ProducibleSize = 1_000_000;

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
    ///     The largest size the engine may declare for <paramref name="type" />, on each side.
    /// </summary>
    /// <remarks>
    ///     Two limits, and the difference between them is what a floor asks for that a ceiling does not.
    ///     <para>
    ///         <b>Producible.</b> Every size member refuses an argument above a million (ADR-0076), because a
    ///         declared bound steers the draw and so every size in range is one the generator may have to
    ///         produce. A 1 MiB body limit is an ordinary domain rule and lands above it, so the engine must
    ///         not write that bound down: it would throw inside the emitted parameterless constructor, where
    ///         no <c>With…</c> call can rescue it. Mirrored here rather than asked of the library, which
    ///         ADR-0063 forbids — and held to that number by a test that pins both sides at once.
    ///     </para>
    ///     <para>
    ///         <b>Distinct.</b> A set or a dictionary draws distinct elements, so a count floor asks the
    ///         element row for that many different values. <c>Any.Enum&lt;Permission&gt;()</c> has three, and a
    ///         floor of five is refused by the library for the same reason <c>JD016</c> reports it. Only the
    ///         two domains the compiler can settle are counted; anything else is unprovable, and an unprovable
    ///         domain must never be treated as a small one.
    ///     </para>
    /// </remarks>
    internal static (bool ByCount, int Ceiling, int Floor) Sizes(ITypeSymbol type) {
        int? distinct = DistinctElements(type);

        return (SizedByCount(type),
                ProducibleSize,
                distinct is null ? ProducibleSize : Math.Min(ProducibleSize, distinct.Value));
    }

    /// <summary>How many different values the element row of a distinct collection can draw, where provable.</summary>
    private static int? DistinctElements(ITypeSymbol type) {
        if (type is not INamedTypeSymbol named || Definition(named) is not { } definition) { return null; }

        bool distinct = definition is "System.Collections.Generic.HashSet`1" or "System.Collections.Generic.ISet`1"
                     || Dictionaries.Contains(definition, StringComparer.Ordinal);

        if (!distinct || named.TypeArguments.Length == 0) { return null; }

        // A dictionary draws distinct KEYS, so it is the key row that is asked for them. Unwrapped, because
        // `ISet<Span?>` draws the same three members `ISet<Span>` does — without this the nullable reads as
        // an ordinary struct and the whole domain is lost.
        ITypeSymbol element = Guards.Underlying(named.TypeArguments[0]);

        if (element.TypeKind == TypeKind.Enum) {
            // DISTINCT constant values, never declared members. `enum Grade { Low = 1, …, Min = 1 }` declares
            // five names for three values, and a floor of five over it is one the element row can never reach.
            int values = element.GetMembers()
                                .OfType<IFieldSymbol>()
                                .Where(field => field.HasConstantValue)
                                .Select(field => field.ConstantValue)
                                .Distinct()
                                .Count();

            return values > 0 ? values : null;
        }

        return PrimitiveCardinality(element);
    }

    /// <summary>
    ///     How many different values the library's unconstrained row for <paramref name="element" /> can
    ///     draw, for the primitive families whose domain is small enough for a distinct count to exhaust.
    /// </summary>
    /// <remarks>
    ///     A mirror, like <see cref="ProducibleSize" />: ADR-0063 keeps the engine from asking the library, so
    ///     it carries the numbers — and <c>ElementCardinalityAgreementTests</c> is what stops them drifting,
    ///     since a copy nothing compares is a copy that goes stale. Only the families a realistic floor can
    ///     exhaust are here. A wider one is left unanswered rather than guessed: reading <c>null</c> as
    ///     "unbounded" is safe for a domain nothing will exhaust, and was the whole defect for the ones it can.
    ///     <para>
    ///         The character row is the ASCII pool of ADR-0075 rather than the 16 bits a <c>char</c> holds:
    ///         what the count has to survive is what the generator draws, not what the type could store.
    ///     </para>
    /// </remarks>
    private static int? PrimitiveCardinality(ITypeSymbol element) {
        return element.SpecialType switch {
            SpecialType.System_Boolean => 2,
            SpecialType.System_Char    => 128,
            SpecialType.System_Byte    => 256,
            SpecialType.System_SByte   => 256,
            SpecialType.System_Int16   => 65536,
            SpecialType.System_UInt16  => 65536,
            _                          => null
        };
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
    internal static ChainReport Chain(DrawnGenerator drawn, IReadOnlyList<GuardConstraint> guards) {
        // The factory's own guards count as declarations alongside the constructor's — never as the table's
        // opinions, which yield under combination where a declaration stands (§5.3).
        List<GuardConstraint> declared = [.. drawn.Tightening, .. guards];

        IReadOnlyList<GuardConstraint> kept = GuardReading.Combine(drawn.Seeded, declared, out bool dropped);

        List<GuardConstraint> written = [.. kept.Where(constraint => LibrarySurface.Carries(drawn.Builder,
                                                                                            constraint.Member,
                                                                                            constraint.Arity))];

        string chain = string.Concat(Ranged(written, drawn.Builder).Select(constraint => constraint.Render()));

        // Read against what was WRITTEN, never against what was read: a guard whose member this generator does
        // not carry survived composition and still reached nothing, and the recap has to say which of the two
        // happened. Compared before the fold, since that rewrites a pair into a call neither half is.
        bool applied     = declared.Any(guard => written.Contains(guard, GuardConstraint.SameCall));
        bool unavailable = kept.Any(constraint => !written.Contains(constraint)
                                               && declared.Contains(constraint, GuardConstraint.SameCall));

        return new ChainReport(drawn.Core + chain + drawn.Suffix, applied, dropped, unavailable);
    }

    /// <summary>
    ///     The two vocabularies a bounded range is spelled in, and the one call that replaces each pair.
    /// </summary>
    /// <remarks>
    ///     Three of <c>JD031</c>'s four; the temporal pair is out of reach because §5.3 never emits a temporal
    ///     bound. The range member is looked up like every other (ADR-0059) — it takes two arguments, so the
    ///     lookup has to say so.
    /// </remarks>
    private static readonly (string Minimum, string Maximum, string Range)[] Ranges = [
        ("WithMinLength", "WithMaxLength", "WithLengthBetween"),
        ("WithMinCount", "WithMaxCount", "WithCountBetween"),
        ("GreaterThanOrEqualTo", "LessThanOrEqualTo", "Between")
    ];

    /// <summary>
    ///     A floor and a ceiling of the same family written as the range they are.
    /// </summary>
    /// <remarks>
    ///     Not a nicety, and not obedience to <c>JD031</c> either: the engine knows it was told an interval, so
    ///     writing the interval is writing what it meant. The two-bound spelling is legal and documented — it
    ///     is how a shared helper sets a floor and a call site adds a ceiling — but nothing here is shared or
    ///     partial, and a reader who is handed both bounds never learns the range form exists.
    ///     <para>
    ///         Only a pair that carries arguments folds. <c>Positive()</c> is a floor with nothing to put in a
    ///         range call, and its exclusive edge has no inclusive spelling on a floating-point type anyway.
    ///     </para>
    /// </remarks>
    private static IReadOnlyList<GuardConstraint> Ranged(IReadOnlyList<GuardConstraint> written, ITypeSymbol? builder) {
        foreach ((string minimum, string maximum, string range) in Ranges) {
            GuardConstraint? floor   = written.FirstOrDefault(constraint => constraint.Member == minimum && constraint.Argument is not null);
            GuardConstraint? ceiling = written.FirstOrDefault(constraint => constraint.Member == maximum && constraint.Argument is not null);

            if (floor is null || ceiling is null) { continue; }
            if (!LibrarySurface.Carries(builder, range, parameters: 2)) { continue; }

            GuardConstraint       ranged = new(range, $"{floor.Argument}, {ceiling.Argument}", Bound.Exact);
            List<GuardConstraint> folded = [];

            foreach (GuardConstraint constraint in written) {
                if (ReferenceEquals(constraint, ceiling)) { continue; }

                folded.Add(ReferenceEquals(constraint, floor) ? ranged : constraint);
            }

            return folded;
        }

        return written;
    }

    /// <summary>The complete expression for a type nothing further will constrain — an element, a key.</summary>
    private string? Resolve(ITypeSymbol type, int remaining, IReadOnlyCollection<ITypeSymbol> underway) {
        DrawnGenerator drawn = Draw(type, remaining, underway);

        return drawn.Resolved ? Chain(drawn, []).Expression : null;
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

        return scalar.Resolved ? scalar : Composed(named, scalar.Provenance);
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
    ///     A type the base table has no row for, drawn through the generator that type owns (§5.4).
    /// </summary>
    /// <remarks>
    ///     One name, whether or not it exists yet. A value object's recipe belongs to the generator scaffolded
    ///     for that type, and deriving it again here — <c>Any.String().NonEmpty().As(OrderReference.Create)</c>
    ///     — would write one copy of that recipe per site composing it, each free to drift from the type's own
    ///     constructor. Where the generator is missing, <c>CS0246</c> at this line names what to scaffold, which
    ///     is ADR-0060's mechanism spelled as a type name rather than as an invented identifier (ADR-0089).
    /// </remarks>
    private DrawnGenerator Composed(INamedTypeSymbol type, Provenance refusal) {
        if (refusal != Provenance.None) { return DrawnGenerator.Unresolved(refusal); }

        INamedTypeSymbol? scaffolded = Composition.ScaffoldedFor(type, compilation, naming);

        if (scaffolded is not null) {
            names.Open(NamespaceOf(scaffolded));

            return DrawnGenerator.From($"new {names.Of(scaffolded)}()", scaffolded, provenance: Provenance.Scaffolded);
        }

        // A generic type's name drops its arguments, so Repository<Order> and Repository<Line> would both be
        // told to write AnyRepository and neither would be the name to write. §5.5 still answers there.
        if (type.IsGenericType) { return DrawnGenerator.Unresolved(); }

        // Named anyway, and deliberately unresolvable: the developer's own build reports it at this line, in the
        // IDE and in CI, the minute the file is written. No builder goes with it, so nothing is chained onto a
        // type this compilation cannot see — and a guard that wanted to be is reported, not dropped (ADR-0059).
        names.Open(NamespaceOf(type));

        return DrawnGenerator.From($"new {TypeNaming.GeneratorNameFor(type, naming)}()",
                                   builder: null,
                                   provenance: Provenance.Scaffolded);
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
