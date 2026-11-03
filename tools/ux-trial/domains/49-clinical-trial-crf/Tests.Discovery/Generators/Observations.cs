using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     Hand-written generators for <see cref="Observed{T}" />, which <c>any generate</c> refuses (it is generic).
/// </summary>
public static class Observations {

    /// <summary>A resolved field: observed (a value of <paramref name="value" />) or missing (any reason).</summary>
    public static IAny<Observed<T>> Resolved<T>(IAny<T> value) where T : struct {
        return Any.Boolean().Then(observed => observed
                                                  ? value.As(Observed<T>.Of)
                                                  : Any.Enum<MissingReason>().As(Observed<T>.Missing));
    }

    /// <summary>A field of a form: not entered yet (<c>null</c>, one draw in ten) or resolved.</summary>
    public static IAny<Observed<T>?> Entered<T>(IAny<T> value) where T : struct {
        return Resolved(value).OrNull();
    }

    /// <summary>A reason for a missing visit date: any but <see cref="MissingReason.NotApplicable" />.</summary>
    public static AnyEnum<MissingReason> ReasonVisitDidNotTakePlace() {
        return Any.Enum<MissingReason>().DifferentFrom(MissingReason.NotApplicable);
    }

}
