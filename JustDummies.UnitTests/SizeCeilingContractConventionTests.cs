#region Usings declarations

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Holds the twelve size constraints — a string's four lengths, a collection's four counts, a dictionary's four
///     counts — to one documented contract, the one <see cref="SizeGuard" /> enforces for all of them: a size is
///     refused when negative, or above <see cref="SizeGuard.MaxProducibleSize" />.
/// </summary>
/// <remarks>
///     <para>
///         The guard is a single funnel precisely so the argument validation cannot drift between the surfaces; the
///         <c>&lt;exception&gt;</c> lines are the part that escaped it. <c>AnyDictionary</c> carries its own copies
///         of the four count methods rather than deriving from <c>AnyCollection</c>, and when the ceiling arrived
///         (ADR-0076) it was documented on some methods and not others, until a reader could find three contracts
///         for one implementation (issue #183). The ceiling is read off the guard's own constant, so the number in
///         the documentation cannot drift from the number in the code either.
///     </para>
///     <para>
///         A text scan, not reflection: a documentation comment does not survive compilation, and the generated
///         XML is not what a reader of the source sees. The scan reaches the sources the same way
///         <c>XmlDocCrefConventionTests</c> does.
///     </para>
/// </remarks>
public sealed class SizeCeilingContractConventionTests {

    /// <summary>
    ///     Each size constraint under the file that declares it, with the parameter its sentence names — or none,
    ///     for the paired-bound form, which speaks of "a bound" — and the noun the ceiling is described with.
    /// </summary>
    private static readonly (string File, string Method, string? Parameter, string Noun)[] SizeConstraints = [
        ("AnyString.cs",     "WithLength",        "length", "length"),
        ("AnyString.cs",     "WithMinLength",     "length", "length"),
        ("AnyString.cs",     "WithMaxLength",     "length", "length"),
        ("AnyString.cs",     "WithLengthBetween", null,     "length"),
        ("AnyCollection.cs", "WithCount",         "count",  "count"),
        ("AnyCollection.cs", "WithMinCount",      "count",  "count"),
        ("AnyCollection.cs", "WithMaxCount",      "count",  "count"),
        ("AnyCollection.cs", "WithCountBetween",  null,     "count"),
        ("AnyDictionary.cs", "WithCount",         "count",  "count"),
        ("AnyDictionary.cs", "WithMinCount",      "count",  "count"),
        ("AnyDictionary.cs", "WithMaxCount",      "count",  "count"),
        ("AnyDictionary.cs", "WithCountBetween",  null,     "count"),
    ];

    private static readonly Regex DocumentedArgumentOutOfRange = new("<exception cref=\"ArgumentOutOfRangeException\">(.*?)</exception>", RegexOptions.Compiled);

    [Fact(DisplayName = "Every size constraint documents the one contract its shared guard enforces: negative, or above the ceiling.")]
    public void EverySizeConstraintDocumentsTheSharedCeiling() {
        List<string> offenders = [];

        foreach ((string file, string method, string? parameter, string noun) in SizeConstraints) {
            string? documented = DocumentedContract(file, method);
            string  expected   = ExpectedContract(parameter, noun);

            if (documented is null) {
                offenders.Add($"{file}: {method} declares no ArgumentOutOfRangeException in its documentation, or the method was not found; expected \"{expected}\".");
            } else if (documented != expected) {
                offenders.Add($"{file}: {method} documents \"{documented}\"; expected \"{expected}\".");
            }
        }

        Check.WithCustomMessage($"{offenders.Count} size constraint(s) document a contract other than the guard's:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}")
             .That(offenders).IsEmpty();
    }

    /// <summary>The sentence every size constraint must carry, spelled from the guard's own ceiling.</summary>
    private static string ExpectedContract(string? parameter, string noun) {
        string subject = parameter is null ? "a bound" : $"<paramref name=\"{parameter}\" />";
        string ceiling = SizeGuard.MaxProducibleSize.ToString(CultureInfo.InvariantCulture);

        return $"Thrown when {subject} is negative or exceeds {ceiling}, the largest {noun} a generator is asked to produce.";
    }

    /// <summary>
    ///     The <c>ArgumentOutOfRangeException</c> sentence documented on <paramref name="method" /> in
    ///     <paramref name="file" />, read off the <c>///</c> block just above its declaration; <c>null</c> when the
    ///     method or the sentence is not there.
    /// </summary>
    private static string? DocumentedContract(string file, string method) {
        string[] lines       = File.ReadAllLines(Path.Combine(RepositoryRoot(), "JustDummies", file));
        Regex    declaration = new($@"^\s*public\s.*\b{Regex.Escape(method)}\(", RegexOptions.None);

        int declared = Array.FindIndex(lines, line => declaration.IsMatch(line));
        if (declared < 0) { return null; }

        for (int index = declared - 1; index >= 0 && lines[index].TrimStart().StartsWith("///", StringComparison.Ordinal); index--) {
            Match match = DocumentedArgumentOutOfRange.Match(lines[index]);
            if (match.Success) { return match.Groups[1].Value; }
        }

        return null;
    }

    private static string RepositoryRoot() {
        AssemblyMetadataAttribute root = typeof(SizeCeilingContractConventionTests).Assembly
                                                                                   .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                                   .Single(metadata => metadata.Key == "RepositoryRoot");

        return Path.GetFullPath(root.Value!);
    }

}
