namespace Repro.Domain;

public sealed class Leg {
    public Leg(int from, int to) {
        From = from;
        To = to;
    }

    public int From { get; }

    public int To { get; }
}
