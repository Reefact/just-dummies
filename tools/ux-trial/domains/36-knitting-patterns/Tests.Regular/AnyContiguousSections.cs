using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     Written by hand: the sections of a pattern. The first starts at row 1, each starts where the previous one ended,
///     the names are distinct whatever their casing, and every row sits on the side a fabric presents at that row number.
///     A rule between sections that no per-parameter recipe expresses, which is why it is a generator of its own.
/// </summary>
public sealed class AnyContiguousSections : IAny<IReadOnlyList<PatternSection>> {

    private readonly IAny<int>          _sectionCount;
    private readonly AnyPatternSection  _section;

    public AnyContiguousSections()
        : this(Any.Int32().Between(1, 4), new AnyPatternSection()) { }

    private AnyContiguousSections(IAny<int> sectionCount, AnyPatternSection section) {
        _sectionCount = sectionCount;
        _section      = section;
    }

    public AnyContiguousSections WithSectionCount(int value) {
        return new AnyContiguousSections(Any.Fixed(value), _section);
    }

    /// <summary>Refines the template every section is drawn from (its name, first row and first side are set by the run).</summary>
    public AnyContiguousSections WithSection(Func<AnyPatternSection, AnyPatternSection> configure) {
        return new AnyContiguousSections(_sectionCount, configure(_section));
    }

    public IReadOnlyList<PatternSection> Generate() {
        int          count = _sectionCount.Generate();
        List<string> names = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, PatternSection.MaxNameLength))
                                .WithCount(count)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Generate();

        List<PatternSection> sections = new List<PatternSection>();
        int                  nextRow  = 1;
        foreach (string name in names) {
            FabricSide side = nextRow % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;
            PatternSection section = _section.WithName(name).WithFirstRow(nextRow).WithFirstSide(side).Generate();

            sections.Add(section);
            nextRow = section.LastRow + 1;
        }

        return sections;
    }

}
