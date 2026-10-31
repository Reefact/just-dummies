using JustDummies;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>
///     A generator whose every draw runs <c>draw</c> — for a value assembled from other draws whose
///     shape depends on one another (a voice filling the meter just drawn). The draws it makes come
///     from the ambient context, so a <c>[Reproducible]</c> seed still replays them.
/// </summary>
internal sealed class Drawn<T> : IAny<T> {

    private readonly Func<T> _draw;

    public Drawn(Func<T> draw) {
        _draw = draw;
    }

    public T Generate() {
        return _draw();
    }

}
