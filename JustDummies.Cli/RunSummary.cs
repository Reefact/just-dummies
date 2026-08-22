using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>What a run came to, in the three numbers a script branches on.</summary>
internal sealed record RunSummary(int Scaffolded, int Failed, int OpenParameters);
