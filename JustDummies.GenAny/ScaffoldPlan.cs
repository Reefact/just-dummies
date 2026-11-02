using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JustDummies.GenAny;

/// <summary>
///     Everything the emitter needs, and nothing it has to work out for itself.
/// </summary>
/// <remarks>
///     The split is what keeps §8.1 honest. Resolution (§5) decides; emission (§4) renders. An emitter that
///     resolved anything of its own — a using it inferred, an ordering it chose — would put a decision outside
///     the golden files that check it, and byte-identity would rest on that decision staying stable.
///     <para>
///         Parameters arrive in declaration order and are emitted in it. Usings arrive in any order and are
///         grouped by the emitter, because grouping them is layout — the same reason the emitter, and not the
///         plan, decides where the blank lines go. Nothing anywhere is ordered by hash.
///     </para>
/// </remarks>
public sealed class ScaffoldPlan {

    /// <summary>
    ///     Declares what is to be emitted.
    /// </summary>
    /// <param name="target">The type a generator is scaffolded for.</param>
    /// <param name="generatorName">The generator's own type name, from <see cref="TypeNaming" />.</param>
    /// <param name="usings">The namespaces the emitted file opens.</param>
    /// <param name="parameters">The construction parameters, in declaration order.</param>
    /// <param name="factory">
    ///     The static factory <c>Generate()</c> calls — <c>Order.Create</c> — or null when it calls the
    ///     constructor (§5.1).
    /// </param>
    /// <param name="pinsThroughAnyFixed">
    ///     Whether a pin is written <c>Any.Fixed(value)</c> — the library the file compiles against carries it — or
    ///     through the nested <c>FixedValue</c> helper the file then declares (ADR-0059, ADR-0118).
    /// </param>
    /// <param name="pairings">
    ///     The parameters drawn two by two through <c>Any.OrderedPair</c>, because a guard ties them by an order
    ///     (§5.3); none when null.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument but <paramref name="factory" /> and <paramref name="pairings" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     A collection carries a null element, the generator name is blank, or a pairing names a parameter the plan
    ///     does not carry, or one another pairing already ties.
    /// </exception>
    public ScaffoldPlan(TargetType                          target,
                        string                              generatorName,
                        IReadOnlyList<string>               usings,
                        IReadOnlyList<ScaffoldedParameter>  parameters,
                        string?                             factory = null,
                        bool                                pinsThroughAnyFixed = true,
                        IReadOnlyList<OrderedPairing>?      pairings = null) {
        if (target is null) { throw new ArgumentNullException(nameof(target)); }
        if (generatorName is null) { throw new ArgumentNullException(nameof(generatorName)); }
        if (usings is null) { throw new ArgumentNullException(nameof(usings)); }
        if (parameters is null) { throw new ArgumentNullException(nameof(parameters)); }

        if (generatorName.Trim().Length == 0) {
            throw new ArgumentException("A generator has a name.", nameof(generatorName));
        }
        if (usings.Any(@using => string.IsNullOrWhiteSpace(@using))) {
            throw new ArgumentException("A using names a namespace.", nameof(usings));
        }
        if (parameters.Any(parameter => parameter is null)) {
            throw new ArgumentException("A parameter row is never absent.", nameof(parameters));
        }

        IReadOnlyList<OrderedPairing> tied = pairings ?? [];
        List<string>                  ties = [.. tied.SelectMany(pairing => new[] { pairing.First, pairing.Second })];

        if (ties.Distinct(StringComparer.Ordinal).Count() != ties.Count
         || ties.Any(name => parameters.All(parameter => parameter.Name != name || parameter.IsUnresolved))) {
            throw new ArgumentException("A pairing ties two resolved parameters of the plan, each in one pairing at most.", nameof(pairings));
        }

        Target        = target;
        GeneratorName = generatorName;
        Usings        = usings;
        Parameters    = parameters;
        Factory             = factory;
        PinsThroughAnyFixed = pinsThroughAnyFixed;
        Pairings            = tied;
    }

    /// <summary>The type a generator is scaffolded for.</summary>
    public TargetType Target { get; }

    /// <summary>The generator's own type name.</summary>
    public string GeneratorName { get; }

    /// <summary>The namespaces the emitted file opens.</summary>
    public IReadOnlyList<string> Usings { get; }

    /// <summary>The construction parameters, in declaration order.</summary>
    public IReadOnlyList<ScaffoldedParameter> Parameters { get; }

    /// <summary>The static factory <c>Generate()</c> calls, or null when it calls the constructor.</summary>
    public string? Factory { get; }

    /// <summary>
    ///     Whether a pin is written <c>Any.Fixed(value)</c>, or through the nested <c>FixedValue</c> helper the file
    ///     declares when the library it compiles against predates <c>Any.Fixed</c> (ADR-0059).
    /// </summary>
    public bool PinsThroughAnyFixed { get; }

    /// <summary>The parameters drawn two by two through <c>Any.OrderedPair</c> (§4.2, §5.3).</summary>
    public IReadOnlyList<OrderedPairing> Pairings { get; }

    /// <summary>The pairing that ties <paramref name="parameter" />, or null when none does.</summary>
    public OrderedPairing? PairingOf(ScaffoldedParameter parameter) {
        if (parameter is null) { throw new ArgumentNullException(nameof(parameter)); }

        return Pairings.FirstOrDefault(pairing => pairing.Ties(parameter.Name));
    }

    /// <summary>
    ///     Whether this is the degenerate shape of §4.2: nothing to draw, so no fields, no private constructor,
    ///     no <c>With</c> methods and no <c>FixedValue</c> helper.
    /// </summary>
    /// <remarks>
    ///     Emitting the two constructors unconditionally would give them the same signature and fail with
    ///     <c>CS0111</c>. The generator is still worth writing: being an <c>IAny&lt;T&gt;</c>, it composes into
    ///     <c>Any.ListOf(…)</c> and <c>Any.Combine(…)</c>, which <c>new Order()</c> does not.
    /// </remarks>
    public bool IsDegenerate => Parameters.Count == 0;

    /// <summary>
    ///     Whether at least one parameter went unresolved or needs its guard verified, so the emitted file
    ///     carries a TODO.
    /// </summary>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool ContainsTodo => Parameters.Any(parameter => parameter.IsUnresolved || parameter.RequiresVerification);

}
