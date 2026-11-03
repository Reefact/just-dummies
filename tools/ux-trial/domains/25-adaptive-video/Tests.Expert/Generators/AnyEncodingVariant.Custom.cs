// Written by hand, beside the scaffolded generator: `any generate --force` leaves this file alone.

namespace AdaptiveVideo.Domain;

public sealed partial class AnyEncodingVariant {

    /// <summary>Pins the bitrate by its value in kbps: what most tests about a variant are about.</summary>
    public AnyEncodingVariant WithBitrateKbps(int kbps) {
        return WithBitrate(new Bitrate(kbps));
    }

}
