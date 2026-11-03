namespace AdaptiveVideo.Domain.Tests;

/// <summary>Literal ladders and variants the tests are about: their bitrates and heights are load-bearing.</summary>
internal static class Fixtures {

    /// <summary>A variant whose height, frame rate and bitrate are chosen; the width is a dummy.</summary>
    public static EncodingVariant Variant(int height, int framesPerSecond, int kbps) {
        return new AnyEncodingVariant().WithHeight(height)
                                       .WithFramesPerSecond(framesPerSecond)
                                       .WithBitrate(new Bitrate(kbps))
                                       .Generate();
    }

    /// <summary>240p 400 kbps, 480p 1 200 kbps, 720p 3 000 kbps, 1080p 6 000 kbps.</summary>
    public static BitrateLadder FourRungLadder() {
        return new BitrateLadder([Variant(240, 30, 400), Variant(480, 30, 1_200), Variant(720, 30, 3_000), Variant(1080, 30, 6_000)]);
    }

}
