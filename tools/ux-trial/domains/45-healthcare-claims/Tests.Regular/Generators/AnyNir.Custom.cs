using Clinical.Domain;

using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyNir {

    /// <summary>A NIR whose year and month digits agree with the birth date, as a <see cref="Patient" /> requires.</summary>
    public AnyNir BornOn(DateOnly birthDate) {
        AnyNir copy = new AnyNir(this);

        copy._value = Any.StringMatching($@"[12]{birthDate.Year % 100:00}{birthDate.Month:00}(\d{{2}}|2[AB])\d{{6}}")
                         .As(body => body + Nir.ComputeKey(body).ToString("00"));

        return copy;
    }

}
