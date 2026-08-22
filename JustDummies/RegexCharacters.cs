namespace JustDummies;

/// <summary>A terminal: one character drawn uniformly from a fixed set (a literal is the singleton case).</summary>
internal sealed class RegexCharacters : RegexNode {

    #region Fields declarations

    private readonly char[] _choices;

    #endregion

    internal RegexCharacters(char[] choices) {
        if (choices is null) { throw new ArgumentNullException(nameof(choices)); }
        _choices = choices;
    }

    /// <summary>The characters this terminal can emit — empty when a class excludes the whole universe.</summary>
    internal int Count => _choices.Length;

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        context.Append(_choices[context.Random.Next(_choices.Length)]);
    }

}
