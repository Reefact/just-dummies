using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace GridLoadFlow.Tests.Discovery;

[Reproducible]
public sealed class EquipmentTests {

    [Fact]
    public void A_new_line_is_in_service() {
        // Arrange
        // Act
        Line anyLine = Any.Line().Generate();

        // Assert
        Assert.True(anyLine.InService);
    }

    [Fact]
    public void A_line_stamps_its_series_admittance_and_half_its_charging_at_each_end() {
        // Arrange
        Line anyLine = Any.Line().WithImpedance(new Impedance(0.0, 0.1)).WithChargingSusceptancePu(0.04).Generate();

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = anyLine.Stamp();

        // Assert
        Assert.Equal(-10.0 + 0.02, ff.Imaginary, 9);
        Assert.Equal(10.0, ft.Imaginary, 9);
        Assert.Equal(ft, tf);
        Assert.Equal(ff, tt);
    }

    [Fact]
    public void A_line_from_a_bus_to_itself_is_refused() {
        // Arrange
        BusId   anyBus      = Any.BusId().Generate();
        AnyLine anyLoopLine = Any.Line().WithFromBus(anyBus).WithToBus(anyBus);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyLoopLine.Generate());

        // Assert
        Assert.Equal("toBus", error.ParamName);
    }

    [Fact]
    public void Raising_the_tap_moves_it_one_step_of_0_0125() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        anyTransformer.RaiseTap();

        // Assert
        Assert.Equal(1.0125, anyTransformer.TapRatio);
    }

    [Fact]
    public void Lowering_the_tap_moves_it_one_step_of_0_0125() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        anyTransformer.LowerTap();

        // Assert
        Assert.Equal(0.9875, anyTransformer.TapRatio);
    }

    [Fact]
    public void A_tap_at_its_upper_end_stop_cannot_be_raised_and_stays_put() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().WithTapRatio(Transformer.MaximumTap).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(anyTransformer.RaiseTap);

        // Assert
        Assert.Equal(1.1, anyTransformer.TapRatio);
    }

}
