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
