// Written by hand, never touched by `any generate Voice --force`.

using JustDummies;

using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

public sealed partial class AnyVoice {

    /// <summary>
    ///     A voice whose entries add up to exactly <paramref name="length" /> — the rule a measure
    ///     enforces on each of its voices — with every entry drawn from <paramref name="entry" />.
    /// </summary>
    public AnyVoice Filling(Duration length, Func<AnyVoiceEntry, AnyVoiceEntry>? entry = null) {
        AnyVoiceEntry entries = (entry ?? (e => e))(new AnyVoiceEntry());

        return new AnyVoice(_number, EntriesFilling(length, entries));
    }

    // Cuts the length on a grid of half its own unit. Distinct cut points give positive pieces, and
    // the pieces add up to the length by construction: nothing is drawn and then thrown away.
    private static IAny<IReadOnlyList<VoiceEntry>> EntriesFilling(Duration length, AnyVoiceEntry entry) {
        long gridDenominator = length.Denominator * 2;
        int  gridUnits       = checked((int)(length.Numerator * 2));

        return Any.SetOf(Any.Int32().Between(1, gridUnits - 1))
                  .WithMaxCount(Math.Min(gridUnits - 1, 7))
                  .As(cuts => {
                      int[]            bounds = [0, .. cuts.Order(), gridUnits];
                      List<VoiceEntry> pieces = new();
                      for (int index = 1; index < bounds.Length; index++) {
                          Duration piece = new(bounds[index] - bounds[index - 1], gridDenominator);
                          pieces.Add(entry.WithDuration(piece).Generate());
                      }

                      return (IReadOnlyList<VoiceEntry>)pieces;
                  });
    }

}
