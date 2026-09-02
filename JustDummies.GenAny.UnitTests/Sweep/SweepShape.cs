using System.Collections.Generic;

namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     One generated domain, and what the sweep is entitled to conclude from its outcome.
/// </summary>
/// <remarks>
///     <see cref="DistinctDemand" /> is the number of distinct element values the domain's own text demands —
///     a size guard on a set, or the single value a scalar parameter needs. It is zero when the domain demands
///     nothing in particular, and the distinctness rule then declines to judge the shape rather than inventing
///     an expectation: what size the library picks for an unguarded collection is the library's business.
/// </remarks>
internal sealed class SweepShape(string name,
                                 string family,
                                 string target,
                                 string domain,
                                 int distinctDemand = 0,
                                 int distinctCapacity = SweepAxes.Unknown,
                                 IReadOnlyList<string>? companions = null,
                                 string? residue = null,
                                 SweepAxes.Element? element = null) {

    /// <summary>The name a finding is reported under, and the name the August survey used.</summary>
    internal string Name { get; } = name;

    /// <summary>Which product this shape came out of.</summary>
    internal string Family { get; } = family;

    /// <summary>The type argument a developer would type after <c>dum generate</c>.</summary>
    internal string Target { get; } = target;

    /// <summary>The whole source the engine reads, vocabulary included.</summary>
    internal string Domain { get; } = domain;

    /// <summary>How many distinct element values the domain's own text demands, or zero.</summary>
    internal int DistinctDemand { get; } = distinctDemand;

    /// <summary>How many the element type holds, or <see cref="SweepAxes.Unknown" />.</summary>
    internal int DistinctCapacity { get; } = distinctCapacity;

    /// <summary>
    ///     The other types this domain expects a generator for, which the sweep scaffolds beside the target.
    /// </summary>
    /// <remarks>
    ///     A composed parameter draws through the generator its own type owns (ADR-0089), and the engine
    ///     writes <c>new AnyTag()</c> for it whether or not <c>AnyTag</c> has been scaffolded yet — the
    ///     developer is expected to scaffold that one too, and the recap says so. A bench that scaffolded only
    ///     the target would meet a <c>CS0246</c> on every composed shape and would be reporting its own
    ///     omission: the file is not what lands in a project, half of it is missing. Declared here, per shape,
    ///     rather than chased out of compiler errors — the sweep wrote the domain, so it knows what it built.
    /// </remarks>
    internal IReadOnlyList<string> Companions { get; } = companions ?? [];

    /// <summary>
    ///     Why this shape's guard is one §9 declares the tool does not see, or null.
    /// </summary>
    /// <remarks>
    ///     A claim about the SPECIFICATION, not a prediction about the engine: it says a reader can find the
    ///     sentence that excuses this shape, and a reviewer can check it. Which is the whole difference
    ///     between declaring a known residue and encoding today's behaviour — the second would turn the sweep
    ///     into a change detector wearing a defect detector's clothes.
    /// </remarks>
    internal string? Residue { get; } = residue;

    /// <summary>The element axis this shape came out of, when it has one.</summary>
    /// <remarks>
    ///     Kept so an entry in <see cref="SweepDefects" /> can say which shapes it claims from the axes
    ///     themselves rather than by parsing their names back apart.
    /// </remarks>
    internal SweepAxes.Element? Element { get; } = element;

    /// <summary>Whether the element is a nullable value type, which the engine reaches through a cast.</summary>
    internal bool NullableValue => Element?.Type.EndsWith('?') == true;

    /// <inheritdoc />
    public override string ToString() {
        return Name;
    }

}
