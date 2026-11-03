using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyIsbn13 {

    /// <summary>An ISBN with the 978 prefix: the only kind that has an ISBN-10 form.</summary>
    public AnyIsbn13 With978Prefix() {
        AnyIsbn13 copy = new AnyIsbn13(this);

        copy._value = Any.StringMatching("978[0-9]{9}").As(Generated.WithIsbn13CheckDigit);

        return copy;
    }

    /// <summary>An ISBN with the 979 prefix: it has no ISBN-10 form.</summary>
    public AnyIsbn13 With979Prefix() {
        AnyIsbn13 copy = new AnyIsbn13(this);

        copy._value = Any.StringMatching("979[0-9]{9}").As(Generated.WithIsbn13CheckDigit);

        return copy;
    }

}
