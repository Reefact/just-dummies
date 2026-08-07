using System.Reflection;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The catalogue and the analyzer describe the same rule set, in both directions (ADR-0052).
/// </summary>
/// <remarks>
///     <para>
///         The descriptors already READ their id, category, title and help link from
///         <see cref="JustDummiesRule" />, so those four values cannot disagree — the compiler will not let them.
///         What no compiler catches is a rule that exists on one side and not the other: a new analyzer whose
///         descriptor was written with a fresh literal, or a catalogue entry for a rule that was withdrawn. Both
///         leave a build green, and both are silent exactly where this product is loudest.
///     </para>
///     <para>
///         Checked from the SHIPPED artifacts by reflection rather than from a list written here, because a list
///         written here is a third transcription of the rule set and would need the same guard.
///     </para>
/// </remarks>
public sealed class CatalogueCoverageTests {

    /// <summary>Every rule the catalogue publishes, read off the nested types it declares.</summary>
    private static IReadOnlyCollection<string> CataloguedIds() {
        return typeof(JustDummiesRule)
               .GetTypeInfo()
               .DeclaredNestedTypes
               .Where(type => type.IsClass && type.IsAbstract && type.IsSealed)
               .Select(type => type.Name)
               .ToArray();
    }

    /// <summary>
    ///     Every rule the shipped analyzers declare, read off their <see cref="DiagnosticAnalyzer.SupportedDiagnostics" />.
    /// </summary>
    /// <remarks>
    ///     That collection rather than the internal descriptor list, and not only because the list is internal: a
    ///     descriptor Roslyn is never told about is a rule the platform does not know exists, so the supported set
    ///     is what a consumer actually meets. It is also the set the release-tracking file (RS2008) is checked
    ///     against, which keeps this test and that one talking about the same thing.
    /// </remarks>
    private static IReadOnlyCollection<string> DescribedIds() {
        return typeof(DiscardedGeneratorResultAnalyzer).GetTypeInfo().Assembly
               .GetTypes()
               .Where(type => !type.GetTypeInfo().IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
               .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
               .SelectMany(analyzer => analyzer.SupportedDiagnostics)
               .Select(descriptor => descriptor.Id)
               .Distinct(StringComparer.Ordinal)
               .ToArray();
    }

    [Fact(DisplayName = "Every rule the analyzer describes is published by the catalogue.")]
    public void TheCatalogueCoversEveryDescribedRule() {
        string[] uncatalogued = DescribedIds().Except(CataloguedIds(), StringComparer.Ordinal)
                                              .OrderBy(id => id, StringComparer.Ordinal)
                                              .ToArray();

        // A rule with no catalogue entry is one a consumer can only suppress with a literal — which the DCAT
        // analyzers then report and cannot offer a fix for, since there is nothing to point at.
        Check.That(uncatalogued)
             .As("rules the analyzer reports that JustDummiesRule does not publish; add them to the catalogue")
             .IsEmpty();
    }

    [Fact(DisplayName = "Every rule the catalogue publishes is described by the analyzer.")]
    public void TheAnalyzerDescribesEveryCataloguedRule() {
        string[] undescribed = CataloguedIds().Except(DescribedIds(), StringComparer.Ordinal)
                                              .OrderBy(id => id, StringComparer.Ordinal)
                                              .ToArray();

        // The other direction is not symmetric in what it means: a catalogued rule nothing reports is a rule a
        // consumer can suppress and never see, which reads as coverage and is not. Withdrawing one is a
        // [Obsolete] carried forward, never a deletion — so this failing means the entry was dropped, or the
        // descriptor was.
        Check.That(undescribed)
             .As("rules JustDummiesRule publishes that no descriptor reports; withdraw them as [Obsolete] rather than deleting them")
             .IsEmpty();
    }

    [Fact(DisplayName = "The catalogue publishes the rules under the identifier its own class name spells.")]
    public void EveryRuleIdMatchesItsDeclaringTypeName() {
        // Id is written nameof(JDxxx), so this cannot fail while that convention holds — which is why it is
        // worth asserting: the day somebody writes the literal instead, this is what says so.
        (string Type, string Id)[] mismatched = typeof(JustDummiesRule)
                                                .GetTypeInfo()
                                                .DeclaredNestedTypes
                                                .Where(type => type.IsClass && type.IsAbstract && type.IsSealed)
                                                .Select(type => (Type: type.Name,
                                                                 Id: (string)type.GetField("Id", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!))
                                                .Where(pair => !string.Equals(pair.Type, pair.Id, StringComparison.Ordinal))
                                                .ToArray();

        Check.That(mismatched).As("catalogue entries whose Id does not match their class name").IsEmpty();
    }

}
