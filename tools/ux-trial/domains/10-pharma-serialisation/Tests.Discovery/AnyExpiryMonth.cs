using JustDummies;

namespace Serialisation.Domain;

/// <summary>Written by hand: <c>any</c> found no single-argument factory on <see cref="ExpiryMonth" />.</summary>
public sealed class AnyExpiryMonth : IAny<ExpiryMonth> {

    public ExpiryMonth Generate() {
        return Any.Combine(Any.Int32().Between(2000, 2099), Any.Int32().Between(1, 12), ExpiryMonth.Of).Generate();
    }

}
