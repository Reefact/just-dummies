#region Usings declarations

using System.Text;

#endregion

namespace JustDummies;

/// <summary>
///     The carrier of a single generation: the seeded random generator to draw from, and the buffer the nodes write
///     into. <see cref="Append" /> enforces a hard length ceiling so no pattern can expand the buffer without bound —
///     whether through a nested unbounded quantifier (<c>(a+)+</c> and the like) or through bounded quantifiers whose
///     product is very large (<c>(a{1000}){1000}</c>). The value is built directly, never generated then retried, but
///     the buffer is still guarded.
/// </summary>
internal sealed class RegexGenerationContext {

    #region Fields declarations

    private readonly StringBuilder _builder = new();
    private readonly int           _limit;

    #endregion

    internal RegexGenerationContext(SeededRandom random, int limit) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        Random = random;
        _limit = limit;
    }

    internal SeededRandom Random { get; }

    internal void Append(char character) {
        if (_builder.Length >= _limit) {
            throw DummyGenerationException.PatternExceedsGenerationLimit(_limit);
        }

        _builder.Append(character);
    }

    internal string Result() {
        return _builder.ToString();
    }

}
