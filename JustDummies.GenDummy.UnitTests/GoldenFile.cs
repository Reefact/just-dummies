using System;
using System.IO;
using System.Linq;
using System.Reflection;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     Compares emitted text against a file committed next to this suite, byte for byte.
/// </summary>
/// <remarks>
///     Hand-rolled rather than a snapshot library, deliberately. What the emitter needs is an exact comparison
///     against a file a reviewer reads as code — and the approved files are read twice here, once as the
///     expected text and once as the input to <see cref="EmittedCodeCompilesTests" />. A dependency would buy a
///     received/approved workflow this reproduces in twenty lines, and the specification's §13.9 asks for a
///     snapshot mechanism rather than a particular package.
///     <para>
///         On a mismatch the received text is written beside the approved one, so accepting a deliberate change
///         is a file move and reviewing it is a diff.
///     </para>
/// </remarks>
internal static class GoldenFile {

    private const string ApprovedSuffix = ".approved.cs";
    private const string ReceivedSuffix = ".received.cs";

    /// <summary>
    ///     Fails unless <paramref name="actual" /> is exactly the approved text for <paramref name="name" />.
    /// </summary>
    internal static void Approve(string name, string actual) {
        string approvedPath = PathOf(name + ApprovedSuffix);
        string receivedPath = PathOf(name + ReceivedSuffix);

        if (!File.Exists(approvedPath)) {
            Write(receivedPath, actual);

            throw new InvalidOperationException($"No approved file for '{name}'. The emitted text was written to "
                                              + $"{receivedPath}; read it, then rename it to {name}{ApprovedSuffix}.");
        }

        // Read as raw text: the approved files are committed with LF endings, which is what the emitter
        // produces on every platform (§8.1), and a comparison that normalised them would stop checking that.
        string approved = File.ReadAllText(approvedPath);

        // Which makes the checkout part of the contract. Git's Windows default rewrites a text file to CRLF,
        // and every golden then fails at once for a reason that has nothing to do with the emitter. Said here
        // rather than left to be re-diagnosed: .gitattributes pins these files, and this is what notices when
        // that pin is lost.
        if (approved.Contains('\r')) {
            throw new InvalidOperationException($"{approvedPath} was checked out with CRLF endings. The emitter "
                                              + "writes LF on every platform, so the comparison cannot pass here. "
                                              + "Check the .gitattributes entry that pins these files to eol=lf.");
        }

        if (string.Equals(approved, actual, StringComparison.Ordinal)) {
            if (File.Exists(receivedPath)) { File.Delete(receivedPath); }

            return;
        }

        Write(receivedPath, actual);

        Check.WithCustomMessage($"{name} differs from its approved file, first at {FirstDifference(approved, actual)}. "
                              + $"The emitted text is in {receivedPath}.")
             .That(actual).IsEqualTo(approved);
    }

    /// <summary>The approved text for <paramref name="name" />, as the compile-the-output tests read it.</summary>
    internal static string ApprovedTextOf(string name) {
        return File.ReadAllText(PathOf(name + ApprovedSuffix));
    }

    /// <summary>Every approved file in the folder, by name, so no golden can be silently left out.</summary>
    internal static string[] All() {
        return Directory.GetFiles(Folder, "*" + ApprovedSuffix)
                        .Select(path => Path.GetFileName(path))
                        .Select(fileName => fileName.Substring(0, fileName.Length - ApprovedSuffix.Length))
                        .OrderBy(name => name, StringComparer.Ordinal)
                        .ToArray();
    }

    private static string Folder => Path.Combine(RepositoryRoot, "JustDummies.GenDummy.UnitTests", "Golden");

    /// <summary>The working tree, for the suites whose fixtures live in it rather than in an assembly.</summary>
    internal static string RepositoryRoot {
        get {
            AssemblyMetadataAttribute root = typeof(GoldenFile).Assembly
                                                               .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                               .Single(metadata => metadata.Key == "RepositoryRoot");

            return root.Value!;
        }
    }

    private static string PathOf(string fileName) {
        return Path.Combine(Folder, fileName);
    }

    private static void Write(string path, string text) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private static string FirstDifference(string approved, string actual) {
        string[] approvedLines = approved.Split('\n');
        string[] actualLines   = actual.Split('\n');

        for (int index = 0; index < Math.Min(approvedLines.Length, actualLines.Length); index++) {
            if (!string.Equals(approvedLines[index], actualLines[index], StringComparison.Ordinal)) {
                return $"line {index + 1} (approved: '{approvedLines[index]}', emitted: '{actualLines[index]}')";
            }
        }

        return $"line {Math.Min(approvedLines.Length, actualLines.Length) + 1} "
             + $"(approved has {approvedLines.Length} lines, emitted has {actualLines.Length})";
    }

}
