namespace Cdw.Domain;

public sealed class Segment {
    public Segment(double length) {
        if (length <= 0) { throw new ArgumentOutOfRangeException(nameof(length)); }
        Length = length;
    }

    public double Length { get; }
}
