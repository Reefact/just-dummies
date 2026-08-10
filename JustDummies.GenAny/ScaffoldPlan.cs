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
    /// <exception cref="ArgumentNullException">Any argument but <paramref name="factory" /> is null.</exception>
    /// <exception cref="ArgumentException">A collection carries a null element, or the generator name is blank.</exception>
    public ScaffoldPlan(TargetType                          target,
                        string                              generatorName,
                        IReadOnlyList<string>               usings,
                        IReadOnlyList<ScaffoldedParameter>  parameters,
                        string?                             factory = null) {
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

        Target        = target;
        GeneratorName = generatorName;
        Usings        = usings;
        Parameters    = parameters;
        Factory       = factory;
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
    ///     Whether this is the degenerate shape of §4.2: nothing to draw, so no fields, no private constructor,
    ///     no <c>With</c> methods and no <c>FixedValue</c> helper.
    /// </summary>
    /// <remarks>
    ///     Emitting the two constructors unconditionally would give them the same signature and fail with
    ///     <c>CS0111</c>. The generator is still worth writing: being an <c>IAny&lt;T&gt;</c>, it composes into
    ///     <c>Any.ListOf(…)</c> and <c>Any.Combine(…)</c>, which <c>new Order()</c> does not.
    /// </remarks>
    public bool IsDegenerate => Parameters.Count == 0;

    /// <summary>Whether at least one parameter went unresolved, so the emitted file carries a TODO.</summary>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool ContainsTodo => Parameters.Any(parameter => parameter.IsUnresolved);

}
