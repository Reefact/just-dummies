// Written by hand, beside the scaffolded file: `any generate EditorialHypothesis --force` leaves it alone.

using System;
using System.Collections.Generic;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

public sealed partial class AnyEditorialHypothesis {

    /// <summary>A conjecture: the editor's own reading, attested by no witness.</summary>
    public AnyEditorialHypothesis AsConjecture() {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor,
                                          new FixedValue<IReadOnlyCollection<WitnessSiglum>>(Array.Empty<WitnessSiglum>()),
                                          new FixedValue<bool>(true),
                                          _rationale);
    }

    /// <summary>Grounds the hypothesis in the given witnesses, which must read <paramref name="reading" /> at the passage.</summary>
    public AnyEditorialHypothesis Reading(string reading, params WitnessSiglum[] supportingWitnesses) {
        return WithProposedReading(reading).WithSupportingWitnesses(supportingWitnesses);
    }

}
