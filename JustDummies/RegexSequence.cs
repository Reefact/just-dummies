namespace JustDummies;

/// <summary>A concatenation: its children in order.</summary>
internal sealed class RegexSequence : RegexNode {

    #region Fields declarations

    private readonly RegexNode[] _parts;

    #endregion

    internal RegexSequence(RegexNode[] parts) {
        if (parts is null) { throw new ArgumentNullException(nameof(parts)); }
        _parts = parts;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        foreach (RegexNode part in _parts) { part.Append(context); }
    }

}
