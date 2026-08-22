#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     A node of the parsed pattern tree. Generation is a direct recursive descent: each node writes the characters it
///     stands for into the <see cref="RegexGenerationContext" />, drawing counts and choices from the seeded random
///     generator — so the whole tree yields exactly one string that matches the pattern, in one pass.
/// </summary>
[SuppressMessage(SonarRule.S1694.Category, SonarRule.S1694.Id, Justification = SuppressionJustification.S1694.ClosedInternalHierarchyRoot)]
internal abstract class RegexNode {

    internal abstract void Append(RegexGenerationContext context);

}
