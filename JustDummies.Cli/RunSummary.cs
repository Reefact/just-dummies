using System.Collections.Generic;
using System.Linq;

using JustDummies.GenDummy;

namespace JustDummies.Cli;

/// <summary>What a run came to, in the four numbers a script branches on.</summary>
/// <remarks>
///     <see cref="OpenParameters" /> and <see cref="ParametersToVerify" /> are disjoint and each match a row
///     state exactly — the first counts the rows reporting <c>resolved: false</c>, the second those reporting
///     <c>requiresVerification: true</c>. Folding the two into one number would let the count disagree with
///     the rows it summarises, which is the one thing a report of §6.1's kind may not do.
/// </remarks>
internal sealed record RunSummary(int Scaffolded, int Failed, int OpenParameters, int ParametersToVerify);
