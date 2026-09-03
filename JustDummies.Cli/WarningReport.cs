using System.Collections.Generic;
using System.Linq;

using JustDummies.GenDummy;

namespace JustDummies.Cli;

/// <summary>A warning the run carried without stopping — the shadowing row of §7.</summary>
internal sealed record WarningReport(string Kind, string Subject, string Other) {

    internal static WarningReport Of(ScaffoldWarning warning) {
        return new WarningReport(warning.Kind.ToString(), warning.Subject, warning.Other);
    }

}
