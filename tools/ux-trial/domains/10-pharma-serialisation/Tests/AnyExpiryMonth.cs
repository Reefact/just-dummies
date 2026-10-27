using JustDummies;

namespace Serialisation.Domain;

/// <summary>Hand-written: `any` refuses ExpiryMonth (its only factory, Of(year, month), takes two arguments).</summary>
public static partial class Arbitrary {

    public static IAny<ExpiryMonth> ExpiryMonth() {
        return Any.Combine(Any.Int32().Between(2000, 2099), Any.Int32().Between(1, 12), Domain.ExpiryMonth.Of);
    }

}
