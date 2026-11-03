using System;

using JustDummies;

namespace Clinical.Domain;

// Written by hand, beside the scaffolded file: `any generate Nir --force` leaves it alone.
public sealed partial class AnyNir {

    /// <summary>A NIR whose year and month are those of <paramref name="birthDate" />: the tie <c>Patient</c> demands.</summary>
    public AnyNir BornOn(DateOnly birthDate) {
        AnyNir copy = new AnyNir(this);

        copy._value = Any.StringMatching($"[12]{birthDate.Year % 100:00}{birthDate.Month:00}([0-9]{{2}}|2[AB])[0-9]{{6}}")
                         .As(body => body + Nir.ComputeKey(body).ToString("00"));

        return copy;
    }

}
