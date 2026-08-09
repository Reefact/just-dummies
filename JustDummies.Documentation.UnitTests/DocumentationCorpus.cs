#region Usings declarations

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The set of documentation pages this suite holds to its contracts, read off the working tree.
/// </summary>
/// <remarks>
///     <para>
///         Scope is the root <c>README</c> pair, the two relocated root files, and everything under
///         <c>doc/handwritten/</c> — the pages a consumer reads AND the pages a maintainer reads.
///     </para>
///     <para>
///         The maintainer half was held back at first, on the belief that the ADR base naming its English pages without
///         a language suffix would have to be settled before the contracts could reach it. Measured, that was wrong:
///         <see cref="TwinOf" /> already resolves both spellings, and the 144 maintainer pages satisfied the translation
///         and link contracts on the day they were brought in — 0 orphans, 0 structural divergences, 0 dead links. What
///         does NOT reach them is the compile contract: their C# is illustrative, in ADRs whose samples are fragments of
///         an argument rather than code anybody runs.
///     </para>
///     <para>
///         The pages are read from disk rather than embedded, for the same reason the seed golden master is copied
///         beside its assembly: the file a failing run names has to be the file a maintainer can open.
///     </para>
/// </remarks>
internal static class DocumentationCorpus {

    private static readonly Lazy<IReadOnlyList<DocumentationPage>> LazyPages = new(ReadPages);

    /// <summary>Every page in scope, ordered by repository-relative path.</summary>
    public static IReadOnlyList<DocumentationPage> Pages => LazyPages.Value;

    /// <summary>The repository root, baked in by the project file at build time.</summary>
    public static string RepositoryRoot { get; } = ResolveRepositoryRoot();

    /// <summary>
    ///     The highest analyzer rule whose pages are grandfathered out of the compile contract. A rule numbered above
    ///     this one is a page written AFTER the contract existed, so it is held to it like any other.
    /// </summary>
    private const int LastGrandfatheredRule = 28;

    /// <summary>
    ///     The <c>JD001</c>–<c>JD028</c> pages, named one by one rather than matched by their directory.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         These pages predate this suite and are written to a different brief: they show <c>Noncompliant</c> code
    ///         on purpose, in FRAGMENTS — a lone member, a piece of a method body — naming symbols that exist only in
    ///         the reader's imagination. Measured by widening the scope over them: 48 of the 56 pages fail, on 348
    ///         diagnostics, and the majority are syntax errors rather than missing symbols. No fixture repairs that;
    ///         only rewriting the samples would, which is a change to the analyzer documentation and deserves its own
    ///         argument (ADR-0055, "Follow-up Actions").
    ///     </para>
    ///     <para>
    ///         Naming them one by one is what keeps the exemption from spreading. Excluding the whole directory would
    ///         hand the same pass to a page nobody has written yet, so a rule added tomorrow would inherit an exemption
    ///         argued entirely from the state of pages written yesterday. A <c>JD029</c> page is in scope from the day
    ///         it is created, and its author meets the contract while writing rather than never.
    ///     </para>
    ///     <para>
    ///         All of them are held to the translation and link contracts, which they already satisfy.
    ///     </para>
    /// </remarks>
    private static readonly IReadOnlyCollection<string> GrandfatheredAnalyzerPages = BuildGrandfatheredAnalyzerPages();

    /// <summary>
    ///     The pages whose C# samples the compile contract applies to: the user documentation, minus the grandfathered
    ///     rule pages.
    /// </summary>
    /// <remarks>
    ///     The maintainer documentation is excluded as a body rather than page by page, because the exclusion is a
    ///     property of what those pages ARE. An ADR quotes C# to carry an argument — a shape, a signature, the line a
    ///     decision turns on — and is explicitly forbidden from documenting how the thing is built (see that base's
    ///     README). Holding those quotations to a contract written for teaching material would make a decision record
    ///     answer to the code it deliberately outlives.
    /// </remarks>
    public static IEnumerable<DocumentationPage> PagesWithCompilableSamples =>
        Pages.Where(page => !page.RelativePath.StartsWith("doc/handwritten/for-maintainers/", StringComparison.Ordinal))
             .Where(page => !GrandfatheredAnalyzerPages.Contains(page.RelativePath));

    private static IReadOnlyCollection<string> BuildGrandfatheredAnalyzerPages() {
        HashSet<string> pages = new(StringComparer.Ordinal);

        for (int rule = 1; rule <= LastGrandfatheredRule; rule++) {
            string identifier = $"JD{rule:D3}";
            pages.Add($"doc/handwritten/for-users/analyzers/{identifier}.en.md");
            pages.Add($"doc/handwritten/for-users/analyzers/{identifier}.fr.md");
        }

        return pages;
    }

    private static string ResolveRepositoryRoot() {
        string? root = typeof(DocumentationCorpus).Assembly
                                                  .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                  .FirstOrDefault(attribute => attribute.Key == "RepositoryRoot")
                                                 ?.Value;

        if (string.IsNullOrEmpty(root)) {
            throw new InvalidOperationException("The RepositoryRoot assembly metadata is missing; the project file is supposed to supply it.");
        }

        return Path.GetFullPath(root);
    }

    /// <summary>
    ///     The pairs whose two halves do not sit side by side: a file GitHub only recognises at the repository root
    ///     keeps its conventional name and place, and its translation lives with the rest of the user documentation.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> RelocatedTwins = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["CONTRIBUTING.md"] = "doc/handwritten/for-users/CONTRIBUTING.fr.md",
        ["SECURITY.md"]     = "doc/handwritten/for-users/SECURITY.fr.md"
    };

    /// <summary>
    ///     The twin of a page, in either direction: <c>page.en.md</c> ↔ <c>page.fr.md</c>, <c>README.md</c> ↔
    ///     <c>README.fr.md</c> for the index files that keep their bare name so GitHub renders them, and the two
    ///     relocated pairs above. The returned path is where the twin BELONGS — it is not guaranteed to exist, which
    ///     is precisely what the pairing test asserts.
    /// </summary>
    public static string TwinOf(string absolutePath) {
        string relative = Path.GetRelativePath(RepositoryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');

        foreach (KeyValuePair<string, string> pair in RelocatedTwins) {
            if (relative.Equals(pair.Key, StringComparison.Ordinal)) { return Path.Combine(RepositoryRoot, pair.Value.Replace('/', Path.DirectorySeparatorChar)); }
            if (relative.Equals(pair.Value, StringComparison.Ordinal)) { return Path.Combine(RepositoryRoot, pair.Key); }
        }

        if (absolutePath.EndsWith(".fr.md", StringComparison.Ordinal)) {
            string stem = absolutePath[..^".fr.md".Length];

            return File.Exists(stem + ".en.md") ? stem + ".en.md" : stem + ".md";
        }

        if (absolutePath.EndsWith(".en.md", StringComparison.Ordinal)) {
            return absolutePath[..^".en.md".Length] + ".fr.md";
        }

        return absolutePath[..^".md".Length] + ".fr.md";
    }

    private static IReadOnlyList<DocumentationPage> ReadPages() {
        List<string> files = [
            Path.Combine(RepositoryRoot, "README.md"),
            Path.Combine(RepositoryRoot, "README.fr.md"),
            Path.Combine(RepositoryRoot, "CONTRIBUTING.md"),
            Path.Combine(RepositoryRoot, "SECURITY.md")
        ];

        string handwritten = Path.Combine(RepositoryRoot, "doc", "handwritten");
        if (Directory.Exists(handwritten)) {
            files.AddRange(Directory.EnumerateFiles(handwritten, "*.md", SearchOption.AllDirectories));
        }

        List<DocumentationPage> pages = [];
        foreach (string file in files.Where(File.Exists).Distinct(StringComparer.Ordinal)) {
            pages.Add(DocumentationPage.Read(file, RepositoryRoot));
        }

        return [.. pages.OrderBy(page => page.RelativePath, StringComparer.Ordinal)];
    }

}

/// <summary>How a C# sample is turned into a compilation unit.</summary>
internal enum SampleMode {

    /// <summary>The default: the sample is a run of statements, wrapped in a method body.</summary>
    Statements,

    /// <summary>The sample declares types or test methods, and is placed at namespace level.</summary>
    Declarations,

    /// <summary>The sample is not compiled at all.</summary>
    Skipped

}

/// <summary>One fenced block of a page, with whatever <c>jd:</c> marker preceded it.</summary>
/// <param name="InfoString">The fence's language tag — <c>csharp</c>, <c>mermaid</c>, <c>bash</c>, … — or empty.</param>
/// <param name="Content">The fenced text, without the fences themselves.</param>
/// <param name="StartLine">The 1-based line of the opening fence, so a failure points at the page.</param>
/// <param name="Mode">How the sample is compiled, if at all.</param>
/// <param name="RequiresNet8">The sample names API that only exists on the net8.0 asset.</param>
/// <param name="AllowedRuleIds">The JustDummies rules this sample is EXPECTED to trip, because it shows an anti-pattern.</param>
/// <param name="Marker">The raw marker text, compared between a page and its twin.</param>
internal sealed record CodeFence(
    string InfoString,
    string Content,
    int StartLine,
    SampleMode Mode,
    bool RequiresNet8,
    IReadOnlyList<string> AllowedRuleIds,
    string Marker) {

    /// <summary>Whether this fence is a C# sample the compile contract applies to.</summary>
    public bool IsCompilableSample => InfoString.Equals("csharp", StringComparison.OrdinalIgnoreCase) && Mode != SampleMode.Skipped;

}

/// <summary>One inline link of a page, outside any fenced block.</summary>
/// <param name="Target">The raw link target, anchor included.</param>
/// <param name="Line">The 1-based line it appears on.</param>
/// <param name="Text">The link's visible text, which is what identifies the language banner.</param>
internal sealed record MarkdownLink(string Target, int Line, string Text) {

    /// <summary>
    ///     Whether this link is the language banner every page opens with. That banner exists precisely TO cross
    ///     languages — it is how a French reader reaches the English original — so it is the one link on a French
    ///     page that must not point at French.
    /// </summary>
    public bool IsLanguageBanner =>
        Text.Contains("English", StringComparison.OrdinalIgnoreCase)
     || Text.Contains("Français", StringComparison.OrdinalIgnoreCase);

}

/// <summary>A single documentation page, parsed into the shapes the contracts are expressed over.</summary>
internal sealed class DocumentationPage {

    private static readonly Regex FenceExpression   = new(@"^\s*(`{3,})\s*(\S*)\s*$", RegexOptions.Compiled);
    private static readonly Regex HeadingExpression = new(@"^(#{1,6})\s+\S", RegexOptions.Compiled);
    private static readonly Regex MarkerExpression  = new(@"^\s*<!--\s*jd:(?<tokens>.*?)\s*-->\s*$", RegexOptions.Compiled);
    private static readonly Regex LinkExpression    = new(@"\[(?<text>[^\]]*)\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);

    private DocumentationPage(string absolutePath, string relativePath, IReadOnlyList<int> headingLevels, IReadOnlyList<CodeFence> fences, IReadOnlyList<MarkdownLink> links) {
        AbsolutePath  = absolutePath;
        RelativePath  = relativePath;
        HeadingLevels = headingLevels;
        Fences        = fences;
        Links         = links;
    }

    /// <summary>The page's absolute path on disk.</summary>
    public string AbsolutePath { get; }

    /// <summary>The page's path relative to the repository root, with forward slashes.</summary>
    public string RelativePath { get; }

    /// <summary>The depth of every heading, in document order — the page's skeleton.</summary>
    public IReadOnlyList<int> HeadingLevels { get; }

    /// <summary>Every fenced block, in document order.</summary>
    public IReadOnlyList<CodeFence> Fences { get; }

    /// <summary>Every inline link outside a fenced block, in document order.</summary>
    public IReadOnlyList<MarkdownLink> Links { get; }

    /// <summary>Whether this page is a French twin.</summary>
    public bool IsFrench => RelativePath.EndsWith(".fr.md", StringComparison.Ordinal);

    /// <summary>Reads and parses one page.</summary>
    public static DocumentationPage Read(string absolutePath, string repositoryRoot) {
        string[] lines = File.ReadAllLines(absolutePath, Encoding.UTF8);

        List<int>           headings = [];
        List<CodeFence>     fences   = [];
        List<MarkdownLink>  links    = [];

        bool          inFence     = false;
        string        openFence   = string.Empty;
        string        infoString  = string.Empty;
        int           fenceStart  = 0;
        StringBuilder fenceBody   = new();

        for (int index = 0; index < lines.Length; index++) {
            string line   = lines[index];
            Match  fence  = FenceExpression.Match(line);

            if (inFence) {
                // A closing fence is a run of backticks at least as long as the opening one, carrying no info string.
                if (fence.Success && fence.Groups[1].Value.Length >= openFence.Length && fence.Groups[2].Value.Length == 0) {
                    fences.Add(BuildFence(infoString, fenceBody.ToString(), fenceStart, lines, fenceStart - 1));
                    inFence = false;
                    fenceBody.Clear();
                } else {
                    fenceBody.AppendLine(line);
                }

                continue;
            }

            if (fence.Success) {
                inFence    = true;
                openFence  = fence.Groups[1].Value;
                infoString = fence.Groups[2].Value;
                fenceStart = index + 1;

                continue;
            }

            Match heading = HeadingExpression.Match(line);
            if (heading.Success) {
                headings.Add(heading.Groups[1].Value.Length);

                continue;
            }

            foreach (Match link in LinkExpression.Matches(line)) {
                links.Add(new MarkdownLink(link.Groups["target"].Value, index + 1, link.Groups["text"].Value));
            }
        }

        if (inFence) {
            throw new InvalidOperationException($"{Relative(absolutePath, repositoryRoot)}: a fenced block opened at line {fenceStart} is never closed.");
        }

        return new DocumentationPage(absolutePath, Relative(absolutePath, repositoryRoot), headings, fences, links);
    }

    private static string Relative(string absolutePath, string repositoryRoot) {
        return Path.GetRelativePath(repositoryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static CodeFence BuildFence(string infoString, string content, int startLine, string[] lines, int openIndex) {
        string marker = FindMarker(lines, openIndex);

        SampleMode   mode        = SampleMode.Statements;
        bool         requiresNet8 = false;
        List<string> allowed      = [];

        foreach (string token in marker.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            if (token.Equals("skip", StringComparison.OrdinalIgnoreCase)) {
                mode = SampleMode.Skipped;
            } else if (token.Equals("declarations", StringComparison.OrdinalIgnoreCase)) {
                mode = SampleMode.Declarations;
            } else if (token.Equals("net8", StringComparison.OrdinalIgnoreCase)) {
                requiresNet8 = true;
            } else if (token.StartsWith("allow=", StringComparison.OrdinalIgnoreCase)) {
                allowed.AddRange(token["allow=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries));
            } else {
                throw new InvalidOperationException($"Unknown jd: marker token '{token}' at line {startLine}. Known tokens: skip, declarations, net8, allow=JD0NN[,JD0NN].");
            }
        }

        return new CodeFence(infoString, content, startLine, mode, requiresNet8, allowed, marker);
    }

    private static string FindMarker(string[] lines, int openIndex) {
        // The marker is the nearest non-blank line above the opening fence, when that line is a jd: comment.
        for (int index = openIndex - 1; index >= 0; index--) {
            if (lines[index].Trim().Length == 0) { continue; }

            Match marker = MarkerExpression.Match(lines[index]);

            return marker.Success ? marker.Groups["tokens"].Value.Trim() : string.Empty;
        }

        return string.Empty;
    }

}
