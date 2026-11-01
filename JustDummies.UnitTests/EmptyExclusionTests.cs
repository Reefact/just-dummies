#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     An empty exclusion is the identity, not a contradiction: every <c>Except</c> accepts an empty list and leaves the
///     generator drawing exactly what it drew, while an empty <c>OneOf</c> — an empty domain — stays refused (ADR-0122).
/// </summary>
public sealed class EmptyExclusionTests {

    private const int Seed = 20261101;

    private enum Suit { Hearts, Spades }

    /// <summary>Every builder carrying <c>Except(params T[])</c>, reached through an <see cref="AnyContext" />.</summary>
    public static TheoryData<string> Builders() {
        TheoryData<string> builders = [];
        foreach (string name in Factories().Keys.OrderBy(name => name, StringComparer.Ordinal)) { builders.Add(name); }

        return builders;
    }

    [Theory(DisplayName = "An empty Except is a no-op: the generator draws exactly what it drew without it.")]
    [MemberData(nameof(Builders))]
    public void AnEmptyExceptIsANoOp(string builder) {
        Func<AnyContext, object> factory = Factories()[builder];

        object plain     = factory(Any.WithSeed(Seed));
        object excepting = WithEmptyExcept(factory(Any.WithSeed(Seed)));

        Check.That(Draws(excepting)).ContainsExactly(Draws(plain));
    }

    [Fact(DisplayName = "Every builder carrying Except is covered.")]
    public void EveryBuilderCarryingExceptIsCovered() {
        // The guard against a builder added later and forgotten here: every public builder type with an
        // Except(params T[]) overload must appear among the factories above.
        HashSet<string> withExcept = typeof(Any).Assembly.GetExportedTypes()
                                                .Where(type => type.Name.StartsWith("Any", StringComparison.Ordinal))
                                                .Where(type => type.GetMethods().Any(method => method.Name == "Except" && method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType.IsArray))
                                                .Select(type => type.IsGenericType ? type.Name.Split('`')[0] : type.Name)
                                                .ToHashSet();

        Check.That(withExcept.Except(Factories().Keys)).IsEmpty();
    }

    [Fact(DisplayName = "An empty OneOf stays refused: it declares an empty domain.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void AnEmptyOneOfStaysRefused() {
        Check.ThatCode(() => Any.Int32().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().OneOf()).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An exclusion set that starts empty needs no conditional at the call site.")]
    public void AnExclusionSetThatStartsEmptyNeedsNoConditional() {
        string[]   taken = [];
        AnyPattern code  = Any.WithSeed(Seed).StringMatching("[A-Z]{2}-[0-9]{4}");

        Check.That(code.Except(taken).Generate()).Matches("[A-Z]{2}-[0-9]{4}");
    }

    private static Dictionary<string, Func<AnyContext, object>> Factories() {
        Dictionary<string, Func<AnyContext, object>> factories = new(StringComparer.Ordinal) {
            ["AnyEnum"]    = any => any.Enum<Suit>(),
            ["AnyOneOf"]   = any => any.OneOf(1, 2, 3),
            ["AnyPattern"] = any => any.StringMatching("[a-z]{3}")
        };

        foreach (MethodInfo method in typeof(AnyContext).GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            if (method.GetParameters().Length != 0 || method.IsGenericMethod || method.IsSpecialName) { continue; }
            if (!method.ReturnType.GetMethods().Any(candidate => candidate.Name == "Except")) { continue; }

            factories[method.ReturnType.Name] = any => method.Invoke(any, null)!;
        }

        return factories;
    }

    private static object WithEmptyExcept(object builder) {
        MethodInfo except = builder.GetType().GetMethods().Single(method => method.Name == "Except" && method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType.IsArray);
        Array      empty  = Array.CreateInstance(except.GetParameters()[0].ParameterType.GetElementType()!, 0);

        return except.Invoke(builder, [empty])!;
    }

    private static List<object?> Draws(object builder) {
        MethodInfo generate = builder.GetType().GetMethod("Generate", Type.EmptyTypes)!;

        // Invoke returns a plain object on the net472 reference assemblies: the lambda's declared return type keeps
        // the list nullable on both targets without a cast the net10 build would call unnecessary.
        return Enumerable.Range(0, 8).Select(object? (_) => generate.Invoke(builder, null)).ToList();
    }

}
