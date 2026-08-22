namespace JustDummies;

/// <summary>An alternation: one branch, chosen uniformly.</summary>
internal sealed class RegexAlternation : RegexNode {

    #region Fields declarations

    private readonly RegexNode[] _branches;

    #endregion

    internal RegexAlternation(RegexNode[] branches) {
        if (branches is null) { throw new ArgumentNullException(nameof(branches)); }
        _branches = branches;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        _branches[context.Random.Next(_branches.Length)].Append(context);
    }

}
