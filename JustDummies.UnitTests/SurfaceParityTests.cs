#region Usings declarations

using System.Reflection;
using System.Threading.Tasks;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Structural guards over the library's two hand-mirrored surfaces. Both are pure reflection, so they add no
///     per-builder maintenance beyond the expectation table encoded here:
///     <list type="number">
///         <item>
///             <b>Mirror parity.</b> Every scalar factory on the static <see cref="Any" /> entry point has an
///             identical instance counterpart on <see cref="AnyContext" />. A scalar factory added to one surface and
///             forgotten on the other would compile and pass every behavioral test, silently shipping a hole in the
///             deterministic surface.
///         </item>
///         <item>
///             <b>Algebra parity.</b> Each builder exposes exactly the constraint method set its family declares. A
///             renamed or missing constraint on one of the cloned numeric or temporal builders would otherwise slip
///             past the copy-paste discipline that keeps the duplication safe.
///         </item>
///         <item>
///             <b>Value-set identity.</b> Every <c>OneOf</c> identifies the set it was given by its <i>values</i>,
///             so re-declaring the same domain is the documented no-op whatever order it is written in. This one
///             draws its material from the builders rather than reading their declarations, because the rule it
///             holds is about behaviour; it is here because it is the same family of drift the two above catch.
///         </item>
///     </list>
///     Composition and collection factories (<c>Combine</c>, <c>ListOf</c>, <c>DictionaryOf</c>, ...) are deliberately
///     <b>not</b> mirrored onto <see cref="AnyContext" />: they inherit the context through their operand sources, so
///     the mirror guard excludes them by construction (they take an <see cref="IAny{T}" /> operand). That is
///     ADR-0102, and the guard holds it both ways: one of them added to the context shows up as "only on
///     AnyContext" and fails. <c>ContextInheritanceTests</c> pins the behaviour the rule promises.
/// </summary>
public sealed class SurfaceParityTests {

    #region Mirror parity: Any <-> AnyContext

    [Fact(DisplayName = "Every Any scalar factory has an identical AnyContext counterpart.")]
    public void AnyAndAnyContextExposeTheSameScalarFactories() {
        HashSet<string> onAny = typeof(Any)
                                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                .Where(IsScalarFactory)
                                .Select(Signature)
                                .ToHashSet();

        HashSet<string> onContext = typeof(AnyContext)
                                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                    .Where(method => !method.IsSpecialName) // drops the Seed property getter
                                    .Select(Signature)
                                    .ToHashSet();

        string[] onlyOnAny     = onAny.Except(onContext).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();
        string[] onlyOnContext = onContext.Except(onAny).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();

        Check.WithCustomMessage($"Scalar factories only on Any: [{string.Join(", ", onlyOnAny)}]; only on AnyContext: [{string.Join(", ", onlyOnContext)}].")
             .That(onlyOnAny.Length + onlyOnContext.Length)
             .IsEqualTo(0);
    }

    // A scalar factory produces a generator from the context's own source: it returns a builder and takes no
    // IAny<> operand. That excludes the composition/collection factories that live only on Any (Combine, ListOf,
    // SetOf, DictionaryOf, PairOf, ...), as well as the three ways to control seeding — WithSeed (returns
    // AnyContext), Reproducibly (returns void/Task) and UseSeed (returns IDisposable). None of those is a
    // generator factory, and AnyContext is not meant to mirror them: it already *is* an explicit deterministic
    // context, so pinning a seed on one would be meaningless.
    private static bool IsScalarFactory(MethodInfo method) {
        if (method.GetParameters().Any(parameter => IsAny(parameter.ParameterType))) { return false; }

        Type returnType = method.ReturnType;

        return returnType != typeof(AnyContext)
            && returnType != typeof(void)
            && returnType != typeof(IDisposable)
            && !typeof(Task).IsAssignableFrom(returnType);
    }

    private static bool IsAny(Type type) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAny<>);
    }

    // Name + generic arity + parameter types + return type, ignoring the static/instance distinction so the two
    // surfaces line up. A drift in any of those four dimensions moves the signature and fails the guard.
    private static string Signature(MethodInfo method) {
        string parameters = string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name));

        return $"{method.Name}`{method.GetGenericArguments().Length}({parameters}) -> {method.ReturnType.Name}";
    }

    #endregion

    #region Algebra parity: per-family constraint sets

    // The constraint vocabulary each family declares, encoded once as data. This table is the specification; the
    // test compares it against what each builder actually exposes through reflection. Ordering here is irrelevant —
    // the test compares sets. The lattice constraint splits what was once one signed-numeric family: only the
    // integers carry MultipleOf, only Decimal carries WithScale, only the temporals carry WithGranularity.

    // Signed integers: the bound/sign vocabulary plus the integer lattice MultipleOf.
    private static readonly string[] SignedIntegerAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "MultipleOf", "OneOf", "Except", "DifferentFrom"
    ];

    // Unsigned integers drop Positive/Negative (meaningless there — NonZero carries the intent); they keep MultipleOf.
    private static readonly string[] UnsignedIntegerAlgebra = [
        "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "MultipleOf", "OneOf", "Except", "DifferentFrom"
    ];

    // Binary floating-point carries the full signed vocabulary but no lattice: a grid of 10^-n over binary floats is a
    // footgun (0.1 is not representable), so MultipleOf/WithScale are deliberately withheld.
    private static readonly string[] FloatingPointAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    // Decimal is the signed vocabulary plus the decimal scale lattice WithScale.
    private static readonly string[] DecimalAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithScale"
    ];

    // TimeSpan is a signed magnitude with a temporal granularity lattice WithGranularity.
    private static readonly string[] TimeSpanAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity"
    ];

    // Instant-like builders rename the bound family to domain vocabulary, with identical inclusive/exclusive
    // semantics, and carry no Positive/Negative/Zero (an instant has no sign). Conditioned like its only
    // consumer — AnyDateOnly exists on .NET 8 and later — so the net472 leg does not carry a field it cannot use.
#if NET8_0_OR_GREATER
    private static readonly string[] InstantAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];
#endif

    // Instants with sub-day tick precision also carry the temporal granularity lattice WithGranularity (DateOnly,
    // already day-resolution, keeps the plain InstantAlgebra).
    private static readonly string[] InstantWithGranularityAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity"
    ];

    // AnyDateTimeOffset additionally exposes the offset dimension (WithOffset/WithOffsetBetween) — the only instant
    // type carrying a second, offset dimension on top of the instant.
    private static readonly string[] InstantWithGranularityAndOffsetAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity", "WithOffset", "WithOffsetBetween"
    ];

    public static TheoryData<Type, string[]> Builders() {
        TheoryData<Type, string[]> data = new();

        // Signed integers carry MultipleOf; the binary floats do not; Decimal carries WithScale; TimeSpan (a signed
        // magnitude) carries WithGranularity — the lattice constraint is what forks the former shared signed family.
        data.Add(typeof(AnyInt32), SignedIntegerAlgebra);
        data.Add(typeof(AnySByte), SignedIntegerAlgebra);
        data.Add(typeof(AnyInt16), SignedIntegerAlgebra);
        data.Add(typeof(AnyInt64), SignedIntegerAlgebra);
        data.Add(typeof(AnyDouble), FloatingPointAlgebra);
        data.Add(typeof(AnySingle), FloatingPointAlgebra);
        data.Add(typeof(AnyDecimal), DecimalAlgebra);
        data.Add(typeof(AnyTimeSpan), TimeSpanAlgebra);

        data.Add(typeof(AnyByte), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt16), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt32), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt64), UnsignedIntegerAlgebra);

        data.Add(typeof(AnyDateTime), InstantWithGranularityAlgebra);
        data.Add(typeof(AnyDateTimeOffset), InstantWithGranularityAndOffsetAlgebra);

        // The remaining scalar builders each carry their own deliberate set.
        data.Add(typeof(AnyBoolean), new[] { "True", "False", "DifferentFrom" });
        data.Add(typeof(AnyGuid), new[] { "NonEmpty", "Empty", "OneOf", "Except", "DifferentFrom" });
        // AnyEnum adds AllowingCombinations, the opt-in widening the draw from the declared members to their
        // combinations — meaningful only for a [Flags] enum, hence a constraint rather than a second factory.
        data.Add(typeof(AnyEnum<DayOfWeek>), new[] { "AllowingCombinations", "OneOf", "Except", "DifferentFrom" });
        // AnyChar mirrors AnyString's character families exactly, minus the shape constraints a single character
        // has no room for and minus WithChars, whose general form here is OneOf. A family added to one surface and
        // forgotten on the other is the drift this pair of rows exists to catch.
        data.Add(typeof(AnyChar), new[] {
            "Alpha", "AlphaNumeric", "Numeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal",
            "WithoutAlpha", "WithoutNumeric", "InUpperCase", "InLowerCase", "OneOf", "Except", "DifferentFrom"
        });

        // AnyString carries the exclusion pair Except/DifferentFrom (met by a bounded redraw, since strings are not
        // ordinal-mapped) and, like every other family, a composable OneOf that returns the builder itself.
        // NotBlank has no AnyChar counterpart, and that is not the drift the row above warns about: it constrains
        // the assembled string rather than the alphabet a character is drawn from, and a single character is either
        // whitespace or it is not — which Whitespaces() and WithoutAlpha() already say there.
        data.Add(typeof(AnyString), new[] {
            "NonEmpty", "NotBlank", "WithLength", "WithMinLength", "WithMaxLength", "WithLengthBetween",
            "StartingWith", "EndingWith", "Containing",
            "Alpha", "AlphaNumeric", "Numeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal",
            "WithoutAlpha", "WithoutNumeric", "WithChars", "InUpperCase", "InLowerCase",
            "OneOf", "Except", "DifferentFrom"
        });

        // AnyPattern carries the type-agnostic trio and nothing else. The pattern is the whole shape, so a shape
        // constraint stays refused — it would mean building in the intersection of two regular languages — while
        // neither the value set nor the exclusion pair builds anything: OneOf supplies the domain and turns the
        // pattern into the test each value passes, and the pair only rejects. This row is what keeps the trio from
        // drifting back to two, which is how the gap issue #185 reported got there.
        data.Add(typeof(AnyPattern), new[] { "OneOf", "Except", "DifferentFrom" });

#if NET8_0_OR_GREATER
        data.Add(typeof(AnyInt128), SignedIntegerAlgebra);
        data.Add(typeof(AnyHalf), FloatingPointAlgebra);
        data.Add(typeof(AnyUInt128), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyDateOnly), InstantAlgebra);
        data.Add(typeof(AnyTimeOnly), InstantWithGranularityAlgebra);
#endif

        return data;
    }

    [Theory(DisplayName = "Each builder exposes exactly its family's constraint method set.")]
    [MemberData(nameof(Builders))]
    public void BuilderExposesExactlyItsFamilyAlgebra(Type builder, string[] expected) {
        // A constraint method is fluent — it returns the builder itself. Generate() (returns the value) and the
        // explicit interface members (not public) are excluded automatically.
        HashSet<string> actual = builder
                                 .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                 .Where(method => method.ReturnType == builder && !method.IsSpecialName)
                                 .Select(method => method.Name)
                                 .ToHashSet();

        string[] missing    = expected.Except(actual).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        string[] unexpected = actual.Except(expected).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Check.WithCustomMessage($"{builder.Name} — missing: [{string.Join(", ", missing)}]; unexpected: [{string.Join(", ", unexpected)}].")
             .That(missing.Length + unexpected.Length)
             .IsEqualTo(0);
    }

    #endregion

    #region Overload parity: the shape of OneOf

    [Fact(DisplayName = "Every OneOf offers both the params and the sequence form, over the same element type.")]
    public void EveryOneOfOffersBothForms() {
        // The algebra table above compares NAMES, so it cannot see an overload set that differs from one builder to
        // the next — which is exactly the drift issue #185 found: one generator of twenty-three carried a sequence
        // form the others did not. A caller reading `OneOf(params …)` on one builder must not have to discover, per
        // builder, whether a set they already hold as a list is accepted. Reflection rather than a hand-kept list:
        // the next family added is covered without touching this test.
        List<string> offenders = [];

        foreach (Type builder in typeof(Any).Assembly
                                            .GetTypes()
                                            .Where(type => type.IsPublic && Implements(type, typeof(IAny<>)))
                                            .OrderBy(type => type.Name, StringComparer.Ordinal)) {
            MethodInfo[] oneOf = builder.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                        .Where(method => method.Name == "OneOf")
                                        .ToArray();
            if (oneOf.Length == 0) { continue; }

            // The element type comes from the params form, so the sequence form is checked against what the builder
            // itself declares rather than against a type this test guessed.
            Type? element = oneOf.Select(method => method.GetParameters()[0].ParameterType)
                                 .FirstOrDefault(parameter => parameter.IsArray)
                                 ?.GetElementType();

            if (element is null) {
                offenders.Add($"{builder.Name} (no params form)");

                continue;
            }

            if (!oneOf.Any(method => method.GetParameters()[0].ParameterType == typeof(IEnumerable<>).MakeGenericType(element))) {
                offenders.Add($"{builder.Name} (no IEnumerable<{element.Name}> form)");
            }
        }

        Check.WithCustomMessage($"These generators do not offer both OneOf forms: {string.Join(", ", offenders)}.")
             .That(offenders)
             .IsEmpty();
    }

    /// <summary>Whether <paramref name="type" /> closes <paramref name="definition" /> at any type argument.</summary>
    private static bool Implements(Type type, Type definition) {
        return type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);
    }

    #endregion

    #region Value-set identity: a OneOf is its values

    // A builder whose factory needs an argument or a type parameter cannot be reached by the reflection below, so it
    // is named here instead. The guard FAILS on a builder it can neither construct nor find here, which is what keeps
    // this list honest: the next generator carrying OneOf either works by reflection or costs one line.
    private static readonly Dictionary<Type, object> ExplicitInstances = new() {
        [typeof(AnyPattern)]  = Any.StringMatching(@"[a-z]{5}"),
        [typeof(AnyEnum<>)]   = Any.Enum<Suit>()
    };

    private enum Suit {

        Clubs,
        Diamonds,
        Hearts,
        Spades

    }

    [Fact(DisplayName = "Every OneOf identifies a value set by its values, not by the call as it was written.")]
    public void EveryOneOfIdentifiesAValueSetByItsValues() {
        // OneOf documents duplicates as ignored and promises nothing about order, so OneOf(a, b), OneOf(b, a) and
        // OneOf(a, b, b) all declare ONE domain, and re-declaring it is the no-op the surface promises. Comparing
        // the rendered call instead refuses two of the three as a second, conflicting set — a conflict naming a
        // constraint the caller has nothing to loosen in. Issue #185 found that on AnyPattern; it was true of eight
        // other builders, which is why the rule now answers to a guard rather than to nine copies of a comment.
        List<string> offenders = [];

        foreach (Type builder in typeof(Any).Assembly
                                            .GetTypes()
                                            .Where(type => type.IsPublic && Implements(type, typeof(IAny<>)))
                                            .OrderBy(type => type.Name, StringComparer.Ordinal)) {
            MethodInfo? oneOf = builder.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                       .FirstOrDefault(method => method.Name == "OneOf" && method.GetParameters()[0].ParameterType.IsArray);
            if (oneOf is null) { continue; }

            object? generator = Instantiate(builder);
            if (generator is null) {
                offenders.Add($"{builder.Name} (no instance: add one to ExplicitInstances)");

                continue;
            }

            // The material is drawn from the builder itself, so the values are ones it genuinely admits — a set this
            // test invented could be refused for reasons that have nothing to do with the rule under guard.
            List<object>? drawn = TwoDistinctValues(generator);
            if (drawn is null) {
                offenders.Add($"{builder.Name} (could not draw two distinct values)");

                continue;
            }

            // Read off the CONSTRUCTED builder, never off the declaration above: on a generic builder the declared
            // parameter is still an open type argument, which no array can be made of.
            MethodInfo declaring = generator.GetType()
                                            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                            .First(method => method.Name == "OneOf" && method.GetParameters()[0].ParameterType.IsArray);
            Type   element  = declaring.GetParameters()[0].ParameterType.GetElementType()!;
            object declared = declaring.Invoke(generator, [Pack(element, drawn[0], drawn[1])])!;

            // The set, written three ways. The first is the declaration; the other two must be no-ops.
            Refusing(declaring, declared, Pack(element, drawn[1], drawn[0]), $"{builder.Name} (a reordered set conflicts)", offenders);
            Refusing(declaring, declared, Pack(element, drawn[0], drawn[1], drawn[1]), $"{builder.Name} (a duplicated set conflicts)", offenders);

            // And the guard must not swallow a REAL second declaration: one value in common is not the same domain.
            if (!Conflicts(declaring, declared, Pack(element, drawn[0]))) {
                offenders.Add($"{builder.Name} (a genuinely different set no longer conflicts)");
            }
        }

        Check.WithCustomMessage($"These generators do not identify a value set by its values: {string.Join("; ", offenders)}.")
             .That(offenders)
             .IsEmpty();
    }

    /// <summary>The builder as the caller would obtain it, or <c>null</c> when this test has no way to build one.</summary>
    private static object? Instantiate(Type builder) {
        Type key = builder.IsGenericType ? builder.GetGenericTypeDefinition() : builder;
        if (ExplicitInstances.TryGetValue(key, out object? named)) { return named; }

        MethodInfo? factory = typeof(Any).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                         .FirstOrDefault(method => !method.IsGenericMethod
                                                                && method.GetParameters().Length == 0
                                                                && method.ReturnType == builder);

        return factory?.Invoke(null, null);
    }

    /// <summary>Two values the builder itself yields, or <c>null</c> when the draw did not separate two in time.</summary>
    private static List<object>? TwoDistinctValues(object generator) {
        MethodInfo   generate = generator.GetType().GetMethod("Generate", Type.EmptyTypes)!;
        List<object> drawn    = [];

        for (int attempt = 0; attempt < 200 && drawn.Count < 2; attempt++) {
            object value = generate.Invoke(generator, null)!;
            if (!drawn.Contains(value)) { drawn.Add(value); }
        }

        return drawn.Count == 2 ? drawn : null;
    }

    private static Array Pack(Type element, params object[] values) {
        Array packed = Array.CreateInstance(element, values.Length);
        for (int index = 0; index < values.Length; index++) { packed.SetValue(values[index], index); }

        return packed;
    }

    private static void Refusing(MethodInfo declaring, object declared, Array values, string offence, List<string> offenders) {
        if (Conflicts(declaring, declared, values)) { offenders.Add(offence); }
    }

    private static bool Conflicts(MethodInfo declaring, object declared, Array values) {
        try {
            declaring.Invoke(declared, [values]);

            return false;
        } catch (TargetInvocationException error) when (error.InnerException is ConflictingAnyConstraintException) {
            return true;
        }
    }

    #endregion

}
