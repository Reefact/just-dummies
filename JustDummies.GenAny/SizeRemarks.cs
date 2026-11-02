using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JustDummies.GenAny;

/// <summary>
///     The sentences a size guard earns under the recap's rows: one past the cap on either side (#362), and one
///     large enough to steer every default draw (#361).
/// </summary>
/// <remarks>
///     Worded here, once, and carried as data on the parameter (§6): the recap, the comment of an open
///     parameter and the machine report print the same sentence. The numbers are the library's own, mirrored
///     as <c>GeneratorFor</c> mirrors the cap (ADR-0063).
/// </remarks>
internal static class SizeRemarks {

    /// <summary>The size the library will not produce past (ADR-0076), as the recap names it.</summary>
    private const int Cap = 1_000_000;

    /// <summary>
    ///     The ceiling from which a guard-derived size steers every default draw to a size a test did not ask
    ///     for: under ADR-0076 a declared maximum steers the draw, and one default draw of a half-million
    ///     elements was measured taking seconds (#361).
    /// </summary>
    internal const int LargeCeiling = 10_000;

    /// <summary>The members a ceiling is written with, each paired with the family it belongs to.</summary>
    private static readonly string[] Ceilings = ["WithMaxCount", "WithMaxLength", "WithCount", "WithLength"];

    /// <summary>The remark of a floor past the cap, the reason the parameter is left open.</summary>
    internal static string FloorBeyondTheCap(decimal floor, bool byCount) {
        return $"the constructor requires at least {Spelled(floor)} {Units(byCount)}; JustDummies caps sizes at {Spelled(Cap)} (ADR-0076), so no draw can satisfy it";
    }

    /// <summary>
    ///     The remark a resolved parameter earns, if any: its ceiling past the cap, or a very large one among the
    ///     constraints that were written — and whether it is the latter.
    /// </summary>
    internal static (string? Remark, bool Large) For(decimal? ceilingBeyondTheCap,
                                                     IReadOnlyList<GuardConstraint> tightening,
                                                     ChainReport written,
                                                     bool byCount,
                                                     string factory) {
        if (ceilingBeyondTheCap is { } beyond) {
            return ($"the guard's ceiling of {Spelled(beyond)} {Units(byCount)} is above the size cap of {Spelled(Cap)} (ADR-0076), so it holds by construction and nothing was written for it", false);
        }

        GuardConstraint? large = tightening.FirstOrDefault(constraint => Ceilings.Contains(constraint.Member)
                                                                       && constraint.Value is >= LargeCeiling
                                                                       && written.Base.Contains(constraint.Member + "("));

        if (large is null) { return (null, false); }

        string max = byCount ? "WithMaxCount" : "WithMaxLength";

        return ($"the guard's ceiling of {Spelled(large.Value!.Value)} {Units(byCount)} steers every default draw toward that size (ADR-0076); consider a smaller default in {factory}, such as .{max}(100), and leave the full ceiling to the test that needs it", true);
    }

    private static string Units(bool byCount) {
        return byCount ? "elements" : "characters";
    }

    private static string Spelled(decimal value) {
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

}
