using JustDummies;

namespace RoboticMilking.Domain;

// Written by hand, beside the scaffolded AnyAnimal.cs: `any generate --force` never touches it.
public sealed partial class AnyAnimal {

    /// <summary>
    ///     Draws <c>lastMilkedAt</c> from <paramref name="generator" />, which replaces the base: a lambda cannot
    ///     say "sometimes never milked" (<c>OrNull()</c> leaves the <c>AnyDateTimeOffset</c> type).
    /// </summary>
    public AnyAnimal WithLastMilkedAt(IAny<DateTimeOffset?> generator) {
        return new AnyAnimal(_id, _lactationNumber, _daysInMilk, _blindQuarters, generator);
    }

}
