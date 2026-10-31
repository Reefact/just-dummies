using System.Collections.Generic;

using JustDummies;

using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Discovery;

/// <summary>Arrange helpers: a tradition holding arbitrary witnesses under the given sigla.</summary>
internal static class Traditions {

    public static Tradition AnyTraditionWith(params WitnessSiglum[] sigla) {
        Tradition tradition = new Tradition(Any.String().NotBlank().WithMaxLength(80).Generate());
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(Any.Witness().WithSiglum(siglum).Generate());
        }

        return tradition;
    }

    /// <summary>Witnesses whose dating spans coincide, so none is certainly older than another.</summary>
    public static Tradition AnyTraditionOfContemporariesWith(params WitnessSiglum[] sigla) {
        Tradition tradition = new Tradition(Any.String().NotBlank().WithMaxLength(80).Generate());
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(Any.Witness().WithSiglum(siglum).WithDatedFrom(1000).WithDatedTo(1100).Generate());
        }

        return tradition;
    }

    public static List<WitnessSiglum> AnyDistinctSigla(int count) {
        return Any.ListOf(Any.WitnessSiglum()).WithCount(count).Distinct().Generate();
    }

}
