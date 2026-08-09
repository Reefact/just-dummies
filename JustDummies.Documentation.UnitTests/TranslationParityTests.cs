#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The translation contract: the French documentation is a twin of the English one, not a subset of it that drifted.
/// </summary>
/// <remarks>
///     <para>
///         A translation goes stale silently. A section added in English and forgotten in French leaves a French reader
///         with documentation that is not wrong, only incomplete — and nothing in a build has ever noticed. What CAN be
///         checked mechanically is the skeleton: the same headings in the same order, the same fenced blocks in the
///         same order, the same opt-out markers. A page that gained a section in one language alone fails all three.
///     </para>
///     <para>
///         What this deliberately does NOT check is meaning: no test can tell a good translation from a plausible one.
///         The skeleton is the part that goes missing, and it is the part a reviewer skims past.
///     </para>
/// </remarks>
public sealed class TranslationParityTests {

    [Fact(DisplayName = "Every English page has a French twin, and every French page an English one.")]
    public void EveryPageIsPaired() {
        List<string> orphans = [];

        foreach (DocumentationPage page in DocumentationCorpus.Pages) {
            string twin = TwinOf(page.AbsolutePath);
            if (!File.Exists(twin)) {
                orphans.Add($"{page.RelativePath} has no twin at {Path.GetFileName(twin)}");
            }
        }

        Check.WithCustomMessage("No page was found; the documentation scan lost its target.")
             .That(DocumentationCorpus.Pages).Not.IsEmpty();

        Check.WithCustomMessage(
                  $"{orphans.Count} page(s) without a translation:{Environment.NewLine}"
                + string.Join(Environment.NewLine, orphans))
             .That(orphans).IsEmpty();
    }

    [Fact(DisplayName = "A page and its French twin share one heading skeleton.")]
    public void TwinsShareTheirHeadingSkeleton() {
        List<string> divergences = [];

        foreach ((DocumentationPage english, DocumentationPage french) in Pairs()) {
            if (!english.HeadingLevels.SequenceEqual(french.HeadingLevels)) {
                divergences.Add(
                    $"{english.RelativePath}: heading levels {Render(english.HeadingLevels)} "
                  + $"but {french.RelativePath} has {Render(french.HeadingLevels)}");
            }
        }

        Check.WithCustomMessage(
                  $"{divergences.Count} page(s) whose translation has a different structure:{Environment.NewLine}"
                + string.Join(Environment.NewLine, divergences))
             .That(divergences).IsEmpty();
    }

    [Fact(DisplayName = "A page and its French twin carry the same fenced blocks, in the same order.")]
    public void TwinsShareTheirFences() {
        List<string> divergences = [];

        foreach ((DocumentationPage english, DocumentationPage french) in Pairs()) {
            List<string> englishFences = [.. english.Fences.Select(fence => fence.InfoString)];
            List<string> frenchFences  = [.. french.Fences.Select(fence => fence.InfoString)];

            if (!englishFences.SequenceEqual(frenchFences, StringComparer.Ordinal)) {
                divergences.Add(
                    $"{english.RelativePath}: fences [{string.Join(", ", englishFences)}] "
                  + $"but {french.RelativePath} has [{string.Join(", ", frenchFences)}]");
            }
        }

        Check.WithCustomMessage(
                  $"{divergences.Count} page(s) whose translation has different code blocks:{Environment.NewLine}"
                + string.Join(Environment.NewLine, divergences))
             .That(divergences).IsEmpty();
    }

    [Fact(DisplayName = "A page and its French twin agree on every jd: marker.")]
    public void TwinsShareTheirMarkers() {
        List<string> divergences = [];

        foreach ((DocumentationPage english, DocumentationPage french) in Pairs()) {
            List<string> englishMarkers = [.. english.Fences.Select(fence => fence.Marker)];
            List<string> frenchMarkers  = [.. french.Fences.Select(fence => fence.Marker)];

            if (!englishMarkers.SequenceEqual(frenchMarkers, StringComparer.Ordinal)) {
                divergences.Add(
                    $"{english.RelativePath}: markers [{string.Join(" | ", englishMarkers)}] "
                  + $"but {french.RelativePath} has [{string.Join(" | ", frenchMarkers)}]");
            }
        }

        Check.WithCustomMessage(
                  $"{divergences.Count} page(s) whose translation opts out differently:{Environment.NewLine}"
                + string.Join(Environment.NewLine, divergences))
             .That(divergences).IsEmpty();
    }

    /// <summary>
    ///     A French page should keep its reader in French. Two exceptions are principled rather than convenient: the
    ///     language banner exists TO cross languages, and a citation of the decision base is a citation — the ADRs
    ///     are maintainer material with a naming convention of their own, and a link into them is naming a decision
    ///     rather than sending the reader off to read English.
    /// </summary>
    [Fact(DisplayName = "A French page links to French pages.")]
    public void FrenchPagesLinkToFrenchPages() {
        List<string> crossings = [];

        foreach (DocumentationPage page in DocumentationCorpus.Pages.Where(page => page.IsFrench)) {
            string directory = Path.GetDirectoryName(page.AbsolutePath)!;

            foreach (MarkdownLink link in page.Links.Where(link => IsRelativeMarkdownLink(link.Target))) {
                if (link.IsLanguageBanner) { continue; }

                string target = StripAnchor(link.Target);
                if (target.EndsWith(".fr.md", StringComparison.Ordinal)) { continue; }
                if (target.Contains("for-maintainers/", StringComparison.Ordinal)) { continue; }

                string twin = TwinOf(Path.GetFullPath(Path.Combine(directory, target)));
                if (File.Exists(twin)) {
                    crossings.Add($"{page.RelativePath}:{link.Line}: links to {target}, whose French twin exists");
                }
            }
        }

        Check.WithCustomMessage(
                  $"{crossings.Count} link(s) sending a French reader to an English page:{Environment.NewLine}"
                + string.Join(Environment.NewLine, crossings))
             .That(crossings).IsEmpty();
    }

    private static IEnumerable<(DocumentationPage English, DocumentationPage French)> Pairs() {
        Dictionary<string, DocumentationPage> byPath = DocumentationCorpus.Pages.ToDictionary(page => page.AbsolutePath, StringComparer.Ordinal);

        foreach (DocumentationPage page in DocumentationCorpus.Pages.Where(page => !page.IsFrench)) {
            if (byPath.TryGetValue(TwinOf(page.AbsolutePath), out DocumentationPage? french)) {
                yield return (page, french);
            }
        }
    }

    private static string TwinOf(string absolutePath) {
        return DocumentationCorpus.TwinOf(absolutePath);
    }

    private static bool IsRelativeMarkdownLink(string target) {
        return !target.StartsWith('#')
            && !target.Contains("://", StringComparison.Ordinal)
            && !target.StartsWith("mailto:", StringComparison.Ordinal)
            && StripAnchor(target).EndsWith(".md", StringComparison.Ordinal);
    }

    private static string StripAnchor(string target) {
        int anchor = target.IndexOf('#', StringComparison.Ordinal);

        return anchor < 0 ? target : target[..anchor];
    }

    private static string Render(IReadOnlyList<int> levels) {
        return $"[{string.Join(", ", levels)}]";
    }

}
