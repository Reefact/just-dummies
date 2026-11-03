using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hand-written: nine arbitrary digits and the check character the domain computes.</summary>
public sealed partial class AnyIsbn10 : IAny<Isbn10> {

    public Isbn10 Generate() {
        string nine = Any.StringMatching("[0-9]{9}").Generate();

        return new Isbn10(nine + Isbn10.ComputeCheckCharacter(nine));
    }

}
