namespace Drafting.Domain;

/// <summary>An ISO 5455 scale: either an enlargement n:1 or a reduction 1:n.</summary>
public sealed class DrawingScale {

    public DrawingScale(int numerator, int denominator) {
        if (numerator < 1) {
            throw new ArgumentOutOfRangeException(nameof(numerator), numerator, "A scale term is at least 1.");
        }
        if (denominator < 1) {
            throw new ArgumentOutOfRangeException(nameof(denominator), denominator, "A scale term is at least 1.");
        }
        if (numerator != 1 && denominator != 1) {
            throw new ArgumentException("An ISO 5455 scale has 1 on one side.", nameof(denominator));
        }

        Numerator   = numerator;
        Denominator = denominator;
    }

    public int Numerator   { get; }
    public int Denominator { get; }

    public double PaperLengthOf(double realMillimetres) {
        return realMillimetres * Numerator / Denominator;
    }

    public override string ToString() {
        return $"{Numerator}:{Denominator}";
    }

}
