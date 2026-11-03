// Written by hand, beside the scaffolded generator: `any generate --force` leaves this file alone.

namespace AdaptiveVideo.Domain;

public sealed partial class AnyAdaptationPolicy {

    /// <summary>
    ///     Keeps the comfort threshold within a buffer's capacity (a rule of the session, not of the policy). Unlike
    ///     <c>WithComfortBufferSeconds(c =&gt; ...)</c>, which takes the place of a pinned comfort threshold, this
    ///     leaves a pin alone: a test that pinned one past the capacity is refused by the session, loudly.
    /// </summary>
    internal AnyAdaptationPolicy WithComfortBufferSecondsAtMost(double capacitySeconds) {
        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._comfortBufferSecondsRecipe                = _comfortBufferSecondsRecipe.LessThanOrEqualTo(capacitySeconds);
        copy._panicBufferSecondsAndComfortBufferSeconds = copy.PanicBufferSecondsAndComfortBufferSeconds();

        return copy;
    }

}
