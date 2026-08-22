using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.CodeAnalysis.CSharp;

namespace JustDummies.GenAny;

/// <summary>
///     One constructor parameter, and the generator expression the engine inferred for it — or the fact that it
///     inferred none.
/// </summary>
/// <remarks>
///     A parameter with no expression is not an error: it is the outcome §5.5 specifies. The emitted file names
///     an identifier that does not exist, so the developer's own build reports it at the exact line, and the
///     file does not compile until they act (ADR-0060).
/// </remarks>
public sealed class ScaffoldedParameter {

    private ScaffoldedParameter(string name,
                                string typeDisplay,
                                string? expression,
                                Provenance provenance,
                                IReadOnlyList<string> candidates) {
        Name        = name;
        TypeDisplay = typeDisplay;
        Expression  = expression;
        Provenance  = provenance;
        Candidates  = candidates;
    }

    /// <summary>The parameter's name, exactly as the constructor declares it.</summary>
    public string Name { get; }

    /// <summary>The parameter's type, as the emitted file must spell it — <c>IReadOnlyList&lt;string&gt;</c>.</summary>
    public string TypeDisplay { get; }

    /// <summary>The inferred generator expression, or null when none was inferred.</summary>
    public string? Expression { get; }

    /// <summary>
    ///     Where that expression came from, and what could not be read while producing it (§6).
    /// </summary>
    /// <remarks>
    ///     Data, not output: the engine decides it, the console renders it. That is what makes the recap
    ///     testable without a console — and what keeps the tool honest about the difference between "inferred"
    ///     and "guessed".
    /// </remarks>
    public Provenance Provenance { get; }

    /// <summary>
    ///     The factories that all qualified, when that is why the parameter is open (§5.4).
    /// </summary>
    /// <remarks>
    ///     Which one the developer meant is theirs to say, so the engine names them rather than picking. Empty
    ///     for every other outcome.
    /// </remarks>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>Whether the emitted file will carry a TODO for this parameter.</summary>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool IsUnresolved => Expression is null;

    /// <summary>
    ///     The name of the <c>With</c> methods that pin or replace this parameter.
    /// </summary>
    /// <remarks>
    ///     The parameter's name with its first letter upper-cased, invariant culture, after a leading <c>_</c> or
    ///     <c>@</c> is stripped (§4.2). Invariant, not current: a Turkish machine would otherwise scaffold
    ///     <c>WithÄ°d</c> where every other machine scaffolds <c>WithId</c>, which breaks the byte-identity §8.1
    ///     promises.
    /// </remarks>
    public string PascalCasedName {
        get {
            string bare = Bare();

            return char.ToUpperInvariant(bare[0]).ToString(CultureInfo.InvariantCulture) + bare.Substring(1);
        }
    }

    /// <summary>
    ///     The parameter's name as the emitted file spells it.
    /// </summary>
    /// <remarks>
    ///     Two things separate this from <see cref="Name" />, and both are load-bearing. Roslyn reports
    ///     <c>@event</c> as <c>event</c>, so the escape has to be put back or the emitted file does not parse at
    ///     all — no named identifier at a line, and nothing for ADR-0060's mechanism to point the developer at.
    ///     And the leading <c>_</c> that §4.2 strips everywhere else is stripped here too, so that a parameter
    ///     named <c>_id</c> cannot carry the same identifier as the field it is copied into: the emitted
    ///     assignment would then be <c>_id = _id</c>, which compiles, leaves the field null, and makes every
    ///     draw throw.
    /// </remarks>
    public string Identifier {
        get {
            string bare = Bare();

            return SyntaxFacts.GetKeywordKind(bare) == SyntaxKind.None ? bare : "@" + bare;
        }
    }

    /// <summary>The field this parameter is copied into.</summary>
    public string FieldName => "_" + Bare();

    /// <summary>
    ///     The name of the private static method that draws this parameter's generator (§4.2).
    /// </summary>
    /// <remarks>
    ///     One method per parameter, called from the public constructor's initializer, rather than the chain
    ///     built inline there: the constructor then reads as a list of names, and whatever a parameter has to
    ///     say for itself is said inside the method that owns it.
    /// </remarks>
    public string FactoryMethodName => PascalCasedName + "Factory";

    /// <summary>The identifier §5.5 emits in place of a generator, which is deliberately undefined.</summary>
    public string TodoIdentifier => "TODO_supply_a_generator_for_" + Bare();

    /// <summary>
    ///     The name past the leading <c>_</c> or <c>@</c> of §4.2, or the whole name when nothing survives it.
    /// </summary>
    /// <remarks>
    ///     One definition, because the three members that read it have to agree: a field named from one bare
    ///     name and a parameter named from another is exactly the collision this guards against.
    /// </remarks>
    private string Bare() {
        string bare = Name.TrimStart('_', '@');

        return bare.Length == 0 ? Name : bare;
    }

    /// <summary>A parameter the engine inferred a generator for.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="typeDisplay" /> is blank.</exception>
    public static ScaffoldedParameter DrawnFrom(string name,
                                                string typeDisplay,
                                                string expression,
                                                Provenance provenance = Provenance.None) {
        if (expression is null) { throw new ArgumentNullException(nameof(expression)); }
        if (expression.Trim().Length == 0) {
            throw new ArgumentException("An inferred parameter carries an expression; use Unresolved for one that does not.",
                                        nameof(expression));
        }

        return new ScaffoldedParameter(Checked(name, nameof(name)),
                                       Checked(typeDisplay, nameof(typeDisplay)),
                                       expression,
                                       provenance,
                                       candidates: []);
    }

    /// <summary>A parameter the engine inferred no generator for (§5.5).</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="typeDisplay" /> is blank.</exception>
    public static ScaffoldedParameter Unresolved(string name,
                                                 string typeDisplay,
                                                 Provenance provenance = Provenance.None,
                                                 IReadOnlyList<string>? candidates = null) {
        return new ScaffoldedParameter(Checked(name, nameof(name)),
                                       Checked(typeDisplay, nameof(typeDisplay)),
                                       expression: null,
                                       provenance,
                                       candidates ?? []);
    }

    private static string Checked(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        if (value.Trim().Length == 0) { throw new ArgumentException("A parameter has a name and a type.", parameterName); }

        return value;
    }

}
