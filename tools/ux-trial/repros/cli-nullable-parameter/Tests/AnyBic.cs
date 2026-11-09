using JustDummies;

namespace NullRepro;

public sealed class AnyBic : IAny<Bic> {
    public Bic Generate() {
        return new Bic(Any.String().WithLength(8).Generate());
    }
}
