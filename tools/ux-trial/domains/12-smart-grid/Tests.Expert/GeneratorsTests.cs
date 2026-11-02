using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

// The generators carry rules of their own (a closing reading after its opening, a commissioning reading on its meter's point...).
// These tests guard that they never draw what the domain would refuse; a failure here is the generator's, not the domain's.
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_drawn_period_is_a_period_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            ConsumptionPeriod anyPeriod = Any.ConsumptionPeriod().Generate();

            Assert.True(anyPeriod.Days >= 1);
            Assert.True(anyPeriod.ConsumedKwh >= 0m);
        }
    }

    [Fact]
    public void Every_drawn_meter_is_commissioned_on_an_actual_reading_of_its_own_point() {
        for (int draw = 0; draw < Draws; draw++) {
            Meter anyMeter = Any.Meter().Generate();

            Assert.Equal(anyMeter.DeliveryPoint, anyMeter.LastReading.DeliveryPoint);
            Assert.Equal(ReadingSource.Actual, anyMeter.LastReading.Source);
        }
    }

    [Fact]
    public void Every_drawn_window_and_curve_is_one_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            OffPeakWindow anyWindow = Any.OffPeakWindow().Generate();
            DailyLoadCurve anyCurve = Any.DailyLoadCurve().Generate();

            Assert.NotEqual(anyWindow.Start, anyWindow.End);
            Assert.Equal(DailyLoadCurve.SlotsPerDay, anyCurve.Points.Count);
        }
    }

}
