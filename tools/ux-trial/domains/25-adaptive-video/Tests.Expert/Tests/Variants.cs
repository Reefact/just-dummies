using AdaptiveVideo.Domain;

using JustDummies;

namespace AdaptiveVideo.Tests.Expert;

/// <summary>Variants whose height, frame rate and bitrate a test chose; their width is arbitrary.</summary>
internal static class Variants {

    internal static EncodingVariant Of(int height, int framesPerSecond, int kbps) {
        return Any.EncodingVariant()
                  .WithHeight(height)
                  .WithFramesPerSecond(framesPerSecond)
                  .WithBitrateKbps(kbps)
                  .Generate();
    }

}
