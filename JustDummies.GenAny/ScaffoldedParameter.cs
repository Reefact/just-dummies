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
                                string? generatorType,
                                string? baseExpression,
                                string conversion,
                                Provenance provenance) {
        Name           = name;
        TypeDisplay    = typeDisplay;
        GeneratorType  = generatorType;
        BaseExpression = baseExpression;
        Conversion     = conversion;
        Provenance     = provenance;
    }

    /// <summary>The parameter's name, exactly as the constructor declares it.</summary>
    public string Name { get; }

    /// <summary>The parameter's type, as the emitted file must spell it — <c>IReadOnlyList&lt;string&gt;</c>.</summary>
    public string TypeDisplay { get; }

    /// <summary>The inferred generator expression, or null when none was inferred.</summary>
    public string? Expression => BaseExpression is null ? null : BaseExpression + Conversion;

    /// <summary>
    ///     The type of the parameter's valid base, as the emitted file spells it — <c>AnyInt32</c>,
    ///     <c>AnyList&lt;string&gt;</c>, <c>AnyOrderReference</c> — or null when none was inferred.
    /// </summary>
    /// <remarks>
    ///     What the parameter's factory returns and what its refining <c>With</c> overload hands the test's
    ///     lambda (ADR-0109). The concrete generator rather than <c>IAny&lt;T&gt;</c>, because the lambda refines
    ///     the base through that generator's own constraints, and <c>IAny&lt;T&gt;</c> carries none.
    /// </remarks>
    public string? GeneratorType { get; }

    /// <summary>
    ///     The inferred expression up to its constraints — the valid base the factory returns — or null when none
    ///     was inferred.
    /// </summary>
    public string? BaseExpression { get; }

    /// <summary>
    ///     The hop that turns the base into a generator of the parameter's own type — <c>.AsNullable()</c> for an
    ///     <c>int?</c> — or the empty string when the base already draws that type.
    /// </summary>
    /// <remarks>
    ///     Applied wherever the factory is called, never inside it: the factory returns the generator the
    ///     constraints were written on, so that a lambda refining it can write more of them, and the hop follows
    ///     the lambda as it follows the constraints (§5.3).
    /// </remarks>
    public string Conversion { get; }

    /// <summary>
    ///     Where that expression came from, and what could not be read while producing it (§6).
    /// </summary>
    /// <remarks>
    ///     Data, not output: the engine decides it, the console renders it. That is what makes the recap
    ///     testable without a console — and what keeps the tool honest about the difference between "inferred"
    ///     and "guessed".
    /// </remarks>
    public Provenance Provenance { get; }

    /// <summary>Whether the emitted file will carry a TODO for this parameter.</summary>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool IsUnresolved => Expression is null;

    /// <summary>
    ///     Whether this parameter's own factory blocks compilation until the developer checks it (§5.5).
    /// </summary>
    /// <remarks>
    ///     Resolved, and carrying <see cref="Provenance.UnreadGuards" />: something before the first assignment
    ///     to state could be a guard the engine could not read, so the expression it drew may be exactly right
    ///     or may be the base recipe a real invariant rejects on some draw, and the engine cannot tell which.
    ///     Distinct from <see cref="IsUnresolved" />, which has no expression to offer at all — here one exists,
    ///     and stays the factory's working base once the developer deletes the line that blocks it.
    /// </remarks>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool RequiresVerification => !IsUnresolved && Provenance.HasFlag(Provenance.UnreadGuards);

    /// <summary>
    ///     The name of the <c>With</c> methods that pin or refine this parameter.
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
    ///     The name of the private static method that holds this parameter's valid base (§4.2).
    /// </summary>
    /// <remarks>
    ///     One method per parameter, called from the public constructor's initializer and from the refining
    ///     <c>With</c> overload alike (ADR-0109). That is what makes it the one address of the base: the
    ///     developer who tightens it there — a guard the engine could not read, a rule the composing type adds —
    ///     tightens the default and every refinement at once.
    ///     <para>
    ///         Named for what it returns — a value the type's own constructor accepts — because that is the
    ///         claim the method makes and the one a reader has to check.
    ///     </para>
    /// </remarks>
    public string FactoryMethodName => "AnyValid" + PascalCasedName;

    /// <summary>The identifier §5.5 emits in place of a generator, which is deliberately undefined.</summary>
    public string TodoIdentifier => "TODO_supply_a_generator_for_" + Bare();

    /// <summary>
    ///     The identifier §5.5 emits above a working generator it is not certain of, equally undefined.
    /// </summary>
    public string VerifyIdentifier => "TODO_verify_the_generator_for_" + Bare();

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
    /// <param name="name">The parameter's name.</param>
    /// <param name="typeDisplay">The parameter's type, as the emitted file spells it.</param>
    /// <param name="generatorType">The type of <paramref name="expression" />, as the emitted file spells it.</param>
    /// <param name="expression">The inferred expression up to its constraints, without <paramref name="conversion" />.</param>
    /// <param name="provenance">Where the expression came from.</param>
    /// <param name="conversion">The hop after the constraints, if the base does not draw the parameter's own type.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="name" />, <paramref name="typeDisplay" />, <paramref name="generatorType" /> or
    ///     <paramref name="expression" /> is blank.
    /// </exception>
    public static ScaffoldedParameter DrawnFrom(string name,
                                                string typeDisplay,
                                                string generatorType,
                                                string expression,
                                                Provenance provenance = Provenance.None,
                                                string conversion = "") {
        if (expression is null) { throw new ArgumentNullException(nameof(expression)); }
        if (conversion is null) { throw new ArgumentNullException(nameof(conversion)); }
        if (expression.Trim().Length == 0) {
            throw new ArgumentException("An inferred parameter carries an expression; use Unresolved for one that does not.",
                                        nameof(expression));
        }

        return new ScaffoldedParameter(Checked(name, nameof(name)),
                                       Checked(typeDisplay, nameof(typeDisplay)),
                                       Checked(generatorType, nameof(generatorType)),
                                       expression,
                                       conversion,
                                       provenance);
    }

    /// <summary>A parameter the engine inferred no generator for (§5.5).</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="typeDisplay" /> is blank.</exception>
    public static ScaffoldedParameter Unresolved(string name, string typeDisplay, Provenance provenance = Provenance.None) {
        return new ScaffoldedParameter(Checked(name, nameof(name)),
                                       Checked(typeDisplay, nameof(typeDisplay)),
                                       generatorType: null,
                                       baseExpression: null,
                                       conversion: string.Empty,
                                       provenance);
    }

    private static string Checked(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        if (value.Trim().Length == 0) { throw new ArgumentException("A parameter has a name and a type.", parameterName); }

        return value;
    }

}
