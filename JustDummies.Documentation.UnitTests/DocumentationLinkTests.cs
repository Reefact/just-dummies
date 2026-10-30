#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The link contract: every relative link in the user documentation resolves to something that exists.
/// </summary>
/// <remarks>
///     A dead link is the cheapest defect to introduce and the most expensive to notice: renaming one page breaks
///     every reference to it, and the build stays green because nothing reads Markdown. External URLs are deliberately
///     NOT checked — a suite that reaches the network fails for reasons that have nothing to do with the change under
///     test, and a link rotting on someone else's server is not something this repository can hold itself to.
/// </remarks>
public sealed class DocumentationLinkTests {

    [Fact(DisplayName = "Every relative link in the user documentation points at a file that exists.")]
    public void EveryRelativeLinkResolves() {
        List<MarkdownLink> checkedLinks = [];
        List<string>       broken       = [];

        foreach (DocumentationPage page in DocumentationCorpus.Pages) {
            string directory = Path.GetDirectoryName(page.AbsolutePath)!;

            foreach (MarkdownLink link in page.Links) {
                if (!IsRepositoryRelative(link.Target)) { continue; }

                checkedLinks.Add(link);

                string target   = StripAnchor(link.Target);
                string resolved = Path.GetFullPath(Path.Combine(directory, Uri.UnescapeDataString(target)));

                if (!File.Exists(resolved) && !Directory.Exists(resolved)) {
                    broken.Add($"{page.RelativePath}:{link.Line}: {link.Target}");
                }
            }
        }

        Check.WithCustomMessage("No relative link was found; the documentation scan lost its target.")
             .That(checkedLinks).Not.IsEmpty();

        Check.WithCustomMessage(
                  $"{broken.Count} dead link(s):{Environment.NewLine}"
                + string.Join(Environment.NewLine, broken))
             .That(broken).IsEmpty();
    }

    [Fact(DisplayName = "A user page that cites a superseded ADR cites its successor on the same line.")]
    public void ASupersededAdrIsCitedWithItsSuccessor() {
        List<string> alone = [];

        // A decision record keeps citing the record it built on; a page written for users must also show the one in force.
        foreach (DocumentationPage page in DocumentationCorpus.Pages.Where(page => page.RelativePath.StartsWith("doc/handwritten/for-users/", StringComparison.Ordinal))) {
            string directory = Path.GetDirectoryName(page.AbsolutePath)!;

            foreach (MarkdownLink link in page.Links) {
                string? successor = SuccessorOf(directory, link.Target);
                if (successor is null) { continue; }

                if (!CitesSuccessor(directory, page.Links.Where(other => other.Line == link.Line), successor)) {
                    alone.Add($"{page.RelativePath}:{link.Line}: {link.Target} (superseded by {Path.GetFileName(successor)})");
                }
            }
        }

        Check.WithCustomMessage(
                  $"{alone.Count} citation(s) of a superseded ADR without its successor on the same line:{Environment.NewLine}"
                + string.Join(Environment.NewLine, alone))
             .That(alone).IsEmpty();
    }

    [Fact(DisplayName = "The successor check accepts the successor's own file, in either language, and nothing that merely shares its number.")]
    public void TheSuccessorCheckComparesResolvedTargets() {
        string directory = Path.Combine(DocumentationCorpus.RepositoryRoot, "doc", "handwritten", "for-users", "generators");
        string successor = SuccessorOf(directory, "../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md")!;

        Check.That(successor).IsNotNull();
        Check.That(CitesSuccessor(directory, [Link("../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md")], successor)).IsTrue();
        Check.That(CitesSuccessor(directory, [Link("../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md#decision")], successor)).IsTrue();
        // Same number, wrong place: a file of that name outside the decision base, and another record numbered alike.
        Check.That(CitesSuccessor(directory, [Link("../guides/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md")], successor)).IsFalse();
        Check.That(CitesSuccessor(directory, [Link("../../for-maintainers/adr/0097-another-record.md")], successor)).IsFalse();
    }

    private static MarkdownLink Link(string target) {
        return new MarkdownLink(target, 1, "ADR-0097");
    }

    /// <summary>
    ///     The English file of the record that superseded the ADR <paramref name="target" /> points at, resolved from the
    ///     link in that ADR's own status line, or <see langword="null" /> when it points at no ADR or at one still in force.
    ///     The status is read from the English twin, which is canonical and the only one whose wording is uniform.
    /// </summary>
    private static string? SuccessorOf(string directory, string target) {
        if (!IsRepositoryRelative(target)) { return null; }

        string record    = Canonical(Path.GetFullPath(Path.Combine(directory, Uri.UnescapeDataString(StripAnchor(target)))));
        string separator = Path.DirectorySeparatorChar.ToString();
        if (!record.Contains($"{separator}adr{separator}", StringComparison.Ordinal) || !File.Exists(record)) { return null; }

        const string marker = "**Status:** Superseded by [ADR-";
        string?      status = File.ReadLines(record).Take(12).FirstOrDefault(line => line.StartsWith(marker, StringComparison.Ordinal));
        if (status is null) { return null; }

        int open  = status.IndexOf("](", StringComparison.Ordinal) + 2;
        int close = status.IndexOf(')', open);

        return Canonical(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(record)!, status[open..close])));
    }

    /// <summary>Whether one of <paramref name="links" /> resolves to <paramref name="successor" />, in either language.</summary>
    private static bool CitesSuccessor(string directory, IEnumerable<MarkdownLink> links, string successor) {
        return links.Where(link => IsRepositoryRelative(link.Target))
                    .Select(link => Canonical(Path.GetFullPath(Path.Combine(directory, Uri.UnescapeDataString(StripAnchor(link.Target))))))
                    .Any(resolved => string.Equals(resolved, successor, StringComparison.Ordinal));
    }

    /// <summary>A record's English path: its French twin and itself name the same decision.</summary>
    private static string Canonical(string path) {
        return path.EndsWith(".fr.md", StringComparison.Ordinal) ? path[..^".fr.md".Length] + ".md" : path;
    }

    private static bool IsRepositoryRelative(string target) {
        return !target.StartsWith('#')
            && !target.StartsWith('/')
            && !target.Contains("://", StringComparison.Ordinal)
            && !target.StartsWith("mailto:", StringComparison.Ordinal)
            && StripAnchor(target).Length > 0;
    }

    private static string StripAnchor(string target) {
        int anchor = target.IndexOf('#', StringComparison.Ordinal);

        return anchor < 0 ? target : target[..anchor];
    }

}
