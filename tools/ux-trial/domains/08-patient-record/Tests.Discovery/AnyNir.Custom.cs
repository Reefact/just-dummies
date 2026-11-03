using System;

using JustDummies;

namespace Clinical.Domain;

// Written by hand, beside the scaffolded file: a NIR carries its holder's birth year and month,
// and a Patient refuses a NIR that disagrees with the birth date.
public sealed partial class AnyNir {

    public AnyNir BornOn(DateOnly birthDate) {
        AnyNir copy = new AnyNir(this);

        string year  = (birthDate.Year % 100).ToString("00");
        string month = birthDate.Month.ToString("00");

        copy._value = Any.StringMatching($"[12]{year}{month}([0-9]{{2}}|2[AB])[0-9]{{6}}").As(body => body + Nir.ComputeKey(body).ToString("00"));

        return copy;
    }

}
