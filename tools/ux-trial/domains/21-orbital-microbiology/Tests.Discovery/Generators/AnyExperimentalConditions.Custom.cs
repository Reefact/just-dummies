using JustDummies;

namespace OrbitalMicrobiology.Domain;

// Written by hand, beside the scaffolded file.
public sealed partial class AnyExperimentalConditions {

    /// <summary>A centrifuged chamber: 0.5 g or more, and the small volume such a chamber takes.</summary>
    public AnyExperimentalConditions Centrifuged() {
        return WithGravityG(g => g.GreaterThanOrEqualTo(ExperimentalConditions.CentrifugeThresholdG))
              .WithVolumeMillilitres(v => v.LessThanOrEqualTo(ExperimentalConditions.MaxCentrifugeVolumeMillilitres));
    }

}
