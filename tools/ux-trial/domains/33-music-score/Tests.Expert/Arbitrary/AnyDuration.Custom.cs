namespace MusicScore.Domain;

public sealed partial class AnyDuration {

    /// <summary>A strictly positive duration: a note or a rest has a length.</summary>
    public AnyDuration NonZero() {
        return WithNumerator(n => n.GreaterThanOrEqualTo(1));
    }

    /// <summary>A denominator that is a power of two (1/2, 3/8, 5/64...): such durations always add up to a notatable one.</summary>
    public AnyDuration OnBinaryGrid() {
        return WithDenominator(d => d.OneOf(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024));
    }

}
