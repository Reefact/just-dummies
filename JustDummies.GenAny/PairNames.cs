using System.Collections.Generic;
using System.Linq;

namespace JustDummies.GenAny;

/// <summary>
///     The identifiers a pair of parameters adds to the emitted file (§4.2), derived from the two parameters' names
///     exactly as the emitter writes them — one place, so the scaffolder can ask whether they collide with anything
///     the file already declares before it decides to draw the pair (#405).
/// </summary>
/// <remarks>
///     A parameter named <c>startPin</c> beside a pair of <c>start</c> and <c>end</c> declares the very field the
///     pair would add, and two pairs can synthesise one name (<c>(a, bAndC)</c> and <c>(aAndB, c)</c>). Such a
///     scaffold failed to compile (<c>CS0102</c>). The pair is then left <c>to verify</c>, as any order the engine
///     does not draw is: a refusal at the edge rather than a renaming scheme nobody can predict from the parameter
///     names (ADR-0046).
/// </remarks>
internal static class PairNames {

    /// <summary>The private method that draws the pair — <c>StartAndEnd()</c>.</summary>
    internal static string Method(OrderedPairing pairing) {
        return ScaffoldedParameter.PascalCasedNameOf(pairing.First) + "And" + ScaffoldedParameter.PascalCasedNameOf(pairing.Second);
    }

    /// <summary>The field a paired parameter's pin is kept in, beside its recipe — <c>_startPin</c>.</summary>
    internal static string Pin(string parameter) {
        return ScaffoldedParameter.FieldNameOf(parameter) + "Pin";
    }

    /// <summary>Everything <paramref name="pairing" /> adds to the file's declaration space.</summary>
    internal static IReadOnlyList<string> Added(OrderedPairing pairing) {
        return [Method(pairing), Pin(pairing.First), Pin(pairing.Second)];
    }

    /// <summary>
    ///     Everything the file declares for its parameters on its own, pairs aside, plus the two members every
    ///     generator has — the identifiers a pair's own must stay clear of.
    /// </summary>
    internal static HashSet<string> Declared(IEnumerable<string> parameters, string generatorName) {
        HashSet<string> declared = [generatorName, "Generate"];

        foreach (string parameter in parameters) {
            string field  = ScaffoldedParameter.FieldNameOf(parameter);
            string pascal = ScaffoldedParameter.PascalCasedNameOf(parameter);

            declared.UnionWith([field, field + "Recipe", "AnyValid" + pascal, "With" + pascal, "Without" + pascal]);
        }

        return declared;
    }

    /// <summary>Whether <paramref name="pairing" /> adds an identifier the file already declares, or one another pair adds.</summary>
    internal static bool Collides(OrderedPairing pairing, HashSet<string> declared, IEnumerable<OrderedPairing> others) {
        HashSet<string> taken = [.. declared, .. others.Where(other => !ReferenceEquals(other, pairing)).SelectMany(Added)];

        return Added(pairing).Any(taken.Contains);
    }

}
