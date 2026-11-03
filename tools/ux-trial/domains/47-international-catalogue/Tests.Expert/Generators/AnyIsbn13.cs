using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     Hand-written: nine arbitrary digits behind a 978 or 979 prefix, closed by the check digit the domain
///     itself computes (the tests that check the computation use known ISBNs written out as literals).
/// </summary>
public sealed partial class AnyIsbn13 : IAny<Isbn13> {

    private readonly IAny<string> _prefix;

    public AnyIsbn13()
        : this(Any.OneOf("978", "979")) { }

    private AnyIsbn13(IAny<string> prefix) {
        _prefix = prefix;
    }

    /// <summary>An ISBN-13 with an ISBN-10 form.</summary>
    public AnyIsbn13 Prefix978() {
        return new AnyIsbn13(Any.Fixed("978"));
    }

    /// <summary>An ISBN-13 first assigned in 2007, with no ISBN-10 form.</summary>
    public AnyIsbn13 Prefix979() {
        return new AnyIsbn13(Any.Fixed("979"));
    }

    public Isbn13 Generate() {
        string twelve = _prefix.Generate() + Any.StringMatching("[0-9]{9}").Generate();

        return new Isbn13(twelve + Isbn13.ComputeCheckDigit(twelve));
    }

}
