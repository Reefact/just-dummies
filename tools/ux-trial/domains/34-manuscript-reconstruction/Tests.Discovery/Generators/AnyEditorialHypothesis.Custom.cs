using JustDummies;

namespace ManuscriptReconstruction.Domain;

// Written by hand, beside the scaffolded file: `any generate --force` leaves it alone.
public sealed partial class AnyEditorialHypothesis {

    /// <summary>A conjecture: attested by no witness, by definition.</summary>
    public AnyEditorialHypothesis AsConjecture() {
        return WithIsConjecture(true);
    }

    /// <summary>A reading grounded in the given witnesses, so not a conjecture.</summary>
    public AnyEditorialHypothesis CitingWitnesses(params string[] sigla) {
        WitnessSiglum[] witnesses = sigla.Select(siglum => new WitnessSiglum(siglum)).ToArray();

        return WithIsConjecture(false).WithSupportingWitnesses(witnesses);
    }

}
