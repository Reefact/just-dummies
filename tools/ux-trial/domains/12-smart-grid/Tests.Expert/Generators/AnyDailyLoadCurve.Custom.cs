using JustDummies;

namespace SmartGrid.Domain;

// Written by hand, beside the scaffolded file: it survives `any generate --force`.
// The list lambda of WithPowers cannot reach the element generator, so a test that needs the powers drawn another way brings its own.
public sealed partial class AnyDailyLoadCurve {

    /// <summary>Draws the power of every slot from <paramref name="power" />, which must itself stay within the meter rating.</summary>
    public AnyDailyLoadCurve WithPowerOfEachSlotDrawnFrom(IAny<decimal> power) {
        AnyDailyLoadCurve copy = new AnyDailyLoadCurve(this);

        copy._pointsRecipe = Any.ListOf(power).WithCount(DailyLoadCurve.SlotsPerDay);
        copy._points       = copy._pointsRecipe.As(ToPoints);

        return copy;
    }

}
