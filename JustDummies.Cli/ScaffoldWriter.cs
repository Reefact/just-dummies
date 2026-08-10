using System;
using System.IO;
using System.Text;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>
///     Puts the emitted file on disk (§3, §7).
/// </summary>
/// <remarks>
///     The engine performs no IO — it returns a file name and a text (ADR-0065) — so this is where the tool
///     becomes something that changes a working tree, and where the one destructive act it can perform is
///     guarded.
/// </remarks>
internal static class ScaffoldWriter {

    /// <summary>
    ///     Writes <paramref name="file" /> into <paramref name="directory" />, unless something is already
    ///     there.
    /// </summary>
    /// <remarks>
    ///     Overwriting is refused rather than confirmed interactively: the file is the developer's, they may
    ///     have edited it, and a scaffolder that silently replaced a hand-edited file once would never be
    ///     trusted again. <c>--force</c> is the sentence that says they know.
    ///     <para>
    ///         The bytes are written as the emitter produced them, in UTF-8 without a byte-order mark and with
    ///         its own line endings — <see cref="File.WriteAllText(string,string)" /> would be the same, but
    ///         saying it here is what keeps §8.1's byte-identity true of the file on disk and not only of the
    ///         string in memory.
    ///     </para>
    /// </remarks>
    internal static WriteOutcome Write(ScaffoldedFile file, string directory, bool force) {
        if (file is null) { throw new ArgumentNullException(nameof(file)); }
        if (directory is null) { throw new ArgumentNullException(nameof(directory)); }

        string path = Path.Combine(directory, file.FileName);

        if (File.Exists(path) && !force) { return WriteOutcome.Refused(path); }

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, file.SourceText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return WriteOutcome.Written(path);
    }

}

/// <summary>Where the file went, or why it did not.</summary>
internal sealed class WriteOutcome {

    private WriteOutcome(string path, bool written) {
        Path      = path;
        Succeeded = written;
    }

    /// <summary>The path that was written, or that already exists.</summary>
    internal string Path { get; }

    /// <summary>Whether the file was written.</summary>
    internal bool Succeeded { get; }

    internal static WriteOutcome Written(string path) {
        return new WriteOutcome(path, written: true);
    }

    internal static WriteOutcome Refused(string path) {
        return new WriteOutcome(path, written: false);
    }

}
