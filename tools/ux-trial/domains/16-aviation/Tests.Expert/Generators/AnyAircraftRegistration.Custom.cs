using JustDummies;

namespace Aviation.Domain.Tests.Expert.Generators;

// Written by hand, beside the scaffolded file: `any generate --force` leaves it alone.
public static class AnyAircraftRegistrationRecipes {

    /// <summary>A mark in the N-number form: the registration of a United States aircraft.</summary>
    public static AnyAircraftRegistration UnitedStates(this AnyAircraftRegistration registrations) {
        return registrations.WithMark(_ => Any.StringMatching(AnyAircraftRegistration.UnitedStatesPattern));
    }

    /// <summary>A mark in the hyphenated form, whose first character is never N.</summary>
    public static AnyAircraftRegistration Foreign(this AnyAircraftRegistration registrations) {
        return registrations.WithMark(_ => Any.StringMatching(AnyAircraftRegistration.HyphenatedPattern));
    }

}
