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
///             <b>Mirror parity.</b> Every scalar factory on the static <see cref="Dummy" /> entry point has an
///             identical instance counterpart on <see cref="DummyContext" />. A scalar factory added to one surface and
///             forgotten on the other would compile and pass every behavioral test, silently shipping a hole in the
///             deterministic surface.
///         </item>
///         <item>
///             <b>Algebra parity.</b> Each builder exposes exactly the constraint method set its family declares. A
///             renamed or missing constraint on one of the cloned numeric or temporal builders would otherwise slip
///             past the copy-paste discipline that keeps the duplication safe.
///         </item>
///     </list>
///     Composition and collection factories (<c>Combine</c>, <c>ListOf</c>, <c>DictionaryOf</c>, ...) are deliberately
///     <b>not</b> mirrored onto <see cref="DummyContext" />: they inherit the context through their operand sources, so
///     the mirror guard excludes them by construction (they take an <see cref="IDummy{T}" /> operand).
/// </summary>
public sealed class SurfaceParityTests {

    #region Mirror parity: Dummy <-> DummyContext

    [Fact(DisplayName = "Every Dummy scalar factory has an identical DummyContext counterpart.")]
    public void AnyAndAnyContextExposeTheSameScalarFactories() {
        HashSet<string> onDummy = typeof(Dummy)
                                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                .Where(IsScalarFactory)
                                .Select(Signature)
                                .ToHashSet();

        HashSet<string> onContext = typeof(DummyContext)
                                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                    .Where(method => !method.IsSpecialName) // drops the Seed property getter
                                    .Select(Signature)
                                    .ToHashSet();

        string[] onlyOnDummy     = onDummy.Except(onContext).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();
        string[] onlyOnContext = onContext.Except(onDummy).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();

        Check.WithCustomMessage($"Scalar factories only on Dummy: [{string.Join(", ", onlyOnDummy)}]; only on DummyContext: [{string.Join(", ", onlyOnContext)}].")
             .That(onlyOnDummy.Length + onlyOnContext.Length)
             .IsEqualTo(0);
    }

    // A scalar factory produces a generator from the context's own source: it returns a builder and takes no
    // IDummy<> operand. That excludes the composition/collection factories that live only on Dummy (Combine, ListOf,
    // SetOf, DictionaryOf, PairOf, ...), as well as the three ways to control seeding — WithSeed (returns
    // DummyContext), Reproducibly (returns void/Task) and UseSeed (returns IDisposable). None of those is a
    // generator factory, and DummyContext is not meant to mirror them: it already *is* an explicit deterministic
    // context, so pinning a seed on one would be meaningless.
    private static bool IsScalarFactory(MethodInfo method) {
        if (method.GetParameters().Any(parameter => IsDummyOperand(parameter.ParameterType))) { return false; }

        Type returnType = method.ReturnType;

        return returnType != typeof(DummyContext)
            && returnType != typeof(void)
            && returnType != typeof(IDisposable)
            && !typeof(Task).IsAssignableFrom(returnType);
    }

    private static bool IsDummyOperand(Type type) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDummy<>);
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
    // consumer — DummyDateOnly exists on .NET 8 and later — so the net472 leg does not carry a field it cannot use.
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

    // DummyDateTimeOffset additionally exposes the offset dimension (WithOffset/WithOffsetBetween) — the only instant
    // type carrying a second, offset dimension on top of the instant.
    private static readonly string[] InstantWithGranularityAndOffsetAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity", "WithOffset", "WithOffsetBetween"
    ];

    public static TheoryData<Type, string[]> Builders() {
        TheoryData<Type, string[]> data = new();

        // Signed integers carry MultipleOf; the binary floats do not; Decimal carries WithScale; TimeSpan (a signed
        // magnitude) carries WithGranularity — the lattice constraint is what forks the former shared signed family.
        data.Add(typeof(DummyInt32), SignedIntegerAlgebra);
        data.Add(typeof(DummySByte), SignedIntegerAlgebra);
        data.Add(typeof(DummyInt16), SignedIntegerAlgebra);
        data.Add(typeof(DummyInt64), SignedIntegerAlgebra);
        data.Add(typeof(DummyDouble), FloatingPointAlgebra);
        data.Add(typeof(DummySingle), FloatingPointAlgebra);
        data.Add(typeof(DummyDecimal), DecimalAlgebra);
        data.Add(typeof(DummyTimeSpan), TimeSpanAlgebra);

        data.Add(typeof(DummyByte), UnsignedIntegerAlgebra);
        data.Add(typeof(DummyUInt16), UnsignedIntegerAlgebra);
        data.Add(typeof(DummyUInt32), UnsignedIntegerAlgebra);
        data.Add(typeof(DummyUInt64), UnsignedIntegerAlgebra);

        data.Add(typeof(DummyDateTime), InstantWithGranularityAlgebra);
        data.Add(typeof(DummyDateTimeOffset), InstantWithGranularityAndOffsetAlgebra);

        // The remaining scalar builders each carry their own deliberate set.
        data.Add(typeof(DummyBoolean), new[] { "True", "False", "DifferentFrom" });
        data.Add(typeof(DummyGuid), new[] { "NonEmpty", "Empty", "OneOf", "Except", "DifferentFrom" });
        // DummyEnum adds AllowingCombinations, the opt-in widening the draw from the declared members to their
        // combinations — meaningful only for a [Flags] enum, hence a constraint rather than a second factory.
        data.Add(typeof(DummyEnum<DayOfWeek>), new[] { "AllowingCombinations", "OneOf", "Except", "DifferentFrom" });
        // DummyChar mirrors DummyString's character families exactly, minus the shape constraints a single character
        // has no room for and minus WithChars, whose general form here is OneOf. A family added to one surface and
        // forgotten on the other is the drift this pair of rows exists to catch.
        data.Add(typeof(DummyChar), new[] {
            "Alpha", "AlphaNumeric", "Numeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal",
            "WithoutAlpha", "WithoutNumeric", "InUpperCase", "InLowerCase", "OneOf", "Except", "DifferentFrom"
        });

        // DummyString carries the exclusion pair Except/DifferentFrom (met by a bounded redraw, since strings are not
        // ordinal-mapped) and, like every other family, a composable OneOf that returns the builder itself.
        // NotBlank has no DummyChar counterpart, and that is not the drift the row above warns about: it constrains
        // the assembled string rather than the alphabet a character is drawn from, and a single character is either
        // whitespace or it is not — which Whitespaces() and WithoutAlpha() already say there.
        data.Add(typeof(DummyString), new[] {
            "NonEmpty", "NotBlank", "WithLength", "WithMinLength", "WithMaxLength", "WithLengthBetween",
            "StartingWith", "EndingWith", "Containing",
            "Alpha", "AlphaNumeric", "Numeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal",
            "WithoutAlpha", "WithoutNumeric", "WithChars", "InUpperCase", "InLowerCase",
            "OneOf", "Except", "DifferentFrom"
        });

#if NET8_0_OR_GREATER
        data.Add(typeof(DummyInt128), SignedIntegerAlgebra);
        data.Add(typeof(DummyHalf), FloatingPointAlgebra);
        data.Add(typeof(DummyUInt128), UnsignedIntegerAlgebra);
        data.Add(typeof(DummyDateOnly), InstantAlgebra);
        data.Add(typeof(DummyTimeOnly), InstantWithGranularityAlgebra);
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

}
