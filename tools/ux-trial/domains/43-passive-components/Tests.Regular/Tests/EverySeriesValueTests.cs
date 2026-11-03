namespace PassiveComponents.Tests.Regular;

/// <summary>
///     Exhaustive walks over the grid (26 decades x every series): no draw here, the whole domain of the
///     value object is small enough to enumerate.
/// </summary>
public sealed class EverySeriesValueTests {

    private static IEnumerable<PreferredValue> AllValues(ESeries series) {
        for (int decade = PreferredValue.MinDecade; decade <= PreferredValue.MaxDecade; decade++) {
            for (int index = 0; index < PreferredValue.ValuesPerDecade(series); index++) {
                yield return new PreferredValue(series, decade, index);
            }
        }
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void Every_value_of_a_series_snaps_to_itself(ESeries series) {
        foreach (PreferredValue value in AllValues(series)) {
            Assert.Equal(value, PreferredValue.Nearest(value.Value, series));
        }
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void Every_value_of_a_series_is_preferred_in_it(ESeries series) {
        foreach (PreferredValue value in AllValues(series)) {
            Assert.True(PreferredValue.IsPreferred(value.Value, series), value.ToString());
        }
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void Walking_up_with_next_visits_every_value_in_strictly_increasing_order(ESeries series) {
        PreferredValue current = new PreferredValue(series, PreferredValue.MinDecade, 0);
        int visited = 1;

        while (true) {
            PreferredValue next;
            try {
                next = current.Next();
            } catch (InvalidOperationException) {
                break;
            }

            Assert.True(current.Value < next.Value, $"{current} then {next}");
            Assert.Equal(current, next.Previous());
            current = next;
            visited++;
        }

        Assert.Equal(new PreferredValue(series, PreferredValue.MaxDecade, PreferredValue.ValuesPerDecade(series) - 1), current);
        Assert.Equal(26 * PreferredValue.ValuesPerDecade(series), visited);
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void The_geometric_mean_of_two_neighbours_snaps_to_one_of_them(ESeries series) {
        foreach (PreferredValue value in AllValues(series).Where(v => v.Decade is -3 or 0 or 7)) {
            PreferredValue upper = value.Next();
            double geometricMean = Math.Sqrt(value.Value * upper.Value);

            PreferredValue snapped = PreferredValue.Nearest(geometricMean, series);

            Assert.True(snapped.Equals(value) || snapped.Equals(upper), $"{geometricMean:R} between {value} and {upper} snapped to {snapped}");
        }
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void A_value_slightly_off_the_grid_snaps_back_to_it(ESeries series) {
        foreach (PreferredValue value in AllValues(series)) {
            Assert.Equal(value, PreferredValue.Nearest(value.Value * 1.001, series));
            Assert.Equal(value, PreferredValue.Nearest(value.Value * 0.999, series));
        }
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Inductor)]
    [InlineData(ComponentKind.Resistor)]
    public void A_code_made_for_a_preferred_value_reads_back_as_that_value_or_does_not_exist(ComponentKind kind) {
        int coded = 0;

        foreach (PreferredValue value in AllValues(ESeries.E24).Where(v => v.Value >= 1e-13)) {
            MarkingCode code;
            try {
                code = MarkingCode.For(value.Value, kind);
            } catch (ArgumentOutOfRangeException) {
                continue;   // outside the range of the kind
            } catch (ArgumentException) {
                continue;   // no code represents it
            }

            Approx.Equal(value.Value, code.Value);
            coded++;
        }

        Assert.True(coded > 20, $"only {coded} E24 values of {kind} have a code");
    }

}
