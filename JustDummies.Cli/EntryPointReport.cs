using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>The entry point emitted beside the generator, and the call it opened (§4.5).</summary>
internal sealed record EntryPointReport(string File, string Call);
