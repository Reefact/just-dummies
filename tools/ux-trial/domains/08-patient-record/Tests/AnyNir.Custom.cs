using JustDummies;

namespace Clinical.Domain;

public sealed partial class AnyNir {

    /// <summary>A NIR whose year and month digits are those of a birth date: a patient's NIR must agree with it.</summary>
    public AnyNir BornIn(int year, int month) {
        string body = $"[12]{year % 100:00}{month:00}([0-9]{{2}}|2[AB])[0-9]{{6}}";

        return new AnyNir(WithControlKey(Any.StringMatching(body)));
    }

}
