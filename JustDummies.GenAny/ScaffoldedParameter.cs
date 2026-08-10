using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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

    private ScaffoldedParameter(string name, string typeDisplay, string? expression) {
        Name        = name;
        TypeDisplay = typeDisplay;
        Expression  = expression;
    }

    /// <summary>The parameter's name, exactly as the constructor declares it.</summary>
    public string Name { get; }

    /// <summary>The parameter's type, as the emitted file must spell it — <c>IReadOnlyList&lt;string&gt;</c>.</summary>
    public string TypeDisplay { get; }

    /// <summary>The inferred generator expression, or null when none was inferred.</summary>
    public string? Expression { get; }

    /// <summary>Whether the emitted file will carry a TODO for this parameter.</summary>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id,
                     Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
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
            string bare = Name.TrimStart('_', '@');

            if (bare.Length == 0) { return Name; }

            return char.ToUpperInvariant(bare[0]).ToString(CultureInfo.InvariantCulture) + bare.Substring(1);
        }
    }

    /// <summary>The field this parameter is copied into.</summary>
    public string FieldName => "_" + Name.TrimStart('_', '@');

    /// <summary>The identifier §5.5 emits in place of a generator, which is deliberately undefined.</summary>
    public string TodoIdentifier => "TODO_supply_a_generator_for_" + Name.TrimStart('_', '@');

    /// <summary>A parameter the engine inferred a generator for.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="typeDisplay" /> is blank.</exception>
    public static ScaffoldedParameter DrawnFrom(string name, string typeDisplay, string expression) {
        if (expression is null) { throw new ArgumentNullException(nameof(expression)); }
        if (expression.Trim().Length == 0) {
            throw new ArgumentException("An inferred parameter carries an expression; use Unresolved for one that does not.",
                                        nameof(expression));
        }

        return new ScaffoldedParameter(Checked(name, nameof(name)), Checked(typeDisplay, nameof(typeDisplay)), expression);
    }

    /// <summary>A parameter the engine inferred no generator for (§5.5).</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> or <paramref name="typeDisplay" /> is blank.</exception>
    public static ScaffoldedParameter Unresolved(string name, string typeDisplay) {
        return new ScaffoldedParameter(Checked(name, nameof(name)), Checked(typeDisplay, nameof(typeDisplay)), expression: null);
    }

    private static string Checked(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        if (value.Trim().Length == 0) { throw new ArgumentException("A parameter has a name and a type.", parameterName); }

        return value;
    }

}
