using System.Linq;

using JustDummies;

namespace MusicScore.Domain.Tests;

/// <summary>
/// Staves whose measures are described by their meter and barlines only; the voices are dummies that fill each meter.
/// A bar is spelled "3/4", with "[" before it for a start repeat and "]3" after it for an end repeat played three times:
/// "[4/4", "4/4]2", "[3/4]2".
/// </summary>
internal static class Structure {

    public static Staff StaffOf(string name, params string[] bars) {
        Measure[] measures = bars.Select((bar, index) => MeasureOf(index + 1, bar)).ToArray();
        AnyStaff  staff    = Arbitrary.Staff().WithMeasures(measures);

        return name is null ? staff.Generate() : staff.WithName(name).Generate();
    }

    public static Staff UnnamedStaff(params string[] bars) { return StaffOf(null!, bars); }

    public static Score ScoreOf(params string[] bars) {
        return Arbitrary.Score().WithStaves(new[] { UnnamedStaff(bars) }).Generate();
    }

    public static Score ScoreAt(Tempo tempo, params string[] bars) {
        return Arbitrary.Score().WithTempo(tempo).WithStaves(new[] { UnnamedStaff(bars) }).Generate();
    }

    private static Measure MeasureOf(int number, string bar) {
        bool   start = bar.StartsWith("[");
        string rest  = bar.TrimStart('[');
        int    close = rest.IndexOf(']');
        int    times = close < 0 ? 0 : int.Parse(rest.Substring(close + 1));
        string meter = close < 0 ? rest : rest.Substring(0, close);

        return Arbitrary.Measure()
                        .WithNumber(number)
                        .WithTimeSignature(Notation.Meter(meter))
                        .WithRepeatStart(start)
                        .WithRepeatEndTimes(times)
                        .Generate();
    }

}
