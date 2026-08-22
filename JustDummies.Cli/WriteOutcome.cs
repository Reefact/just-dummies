using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using JustDummies.GenAny;

namespace JustDummies.Cli;

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
