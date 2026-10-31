// Scaffolded by `any` (JustDummies), then reshaped by hand: the scaffold drew id, width and height
// independently, but the constructor ties all three (the id declares the height, the width is at least
// the height). The id is therefore derived, never drawn: '<height>p<fps>'. Do NOT re-run
// `any generate EncodingVariant --force` on this file.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>A generator of arbitrary, well-formed <see cref="EncodingVariant" /> values.</summary>
public sealed partial class AnyEncodingVariant : IAny<EncodingVariant> {

    private static readonly int[] FrameRates = [24, 25, 30, 50, 60];

    private readonly IAny<int>     _height;
    private readonly IAny<int>     _framesPerSecond;
    private readonly IAny<int>?    _width;
    private readonly IAny<Bitrate> _bitrate;

    public AnyEncodingVariant()
        : this(height: AnyValidHeight(), framesPerSecond: AnyValidFramesPerSecond(), width: null, bitrate: new AnyBitrate()) { }

    /// <summary>Even heights from 100 (the id needs three digits) to 4320 lines.</summary>
    internal static AnyInt32 AnyValidHeight() {
        return Any.Int32().Between(100, 4320).MultipleOf(2);
    }

    private static AnyInt32 AnyValidFramesPerSecond() {
        return Any.Int32().OneOf(FrameRates);
    }

    private AnyEncodingVariant(IAny<int> height, IAny<int> framesPerSecond, IAny<int>? width, IAny<Bitrate> bitrate) {
        _height          = height;
        _framesPerSecond = framesPerSecond;
        _width           = width;
        _bitrate         = bitrate;
    }

    public AnyEncodingVariant WithHeight(int value) {
        return new AnyEncodingVariant(new FixedValue<int>(value), _framesPerSecond, _width, _bitrate);
    }

    public AnyEncodingVariant WithHeight(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEncodingVariant(configure(AnyValidHeight()), _framesPerSecond, _width, _bitrate);
    }

    public AnyEncodingVariant WithFramesPerSecond(int value) {
        return new AnyEncodingVariant(_height, new FixedValue<int>(value), _width, _bitrate);
    }

    public AnyEncodingVariant WithWidth(int value) {
        return new AnyEncodingVariant(_height, _framesPerSecond, new FixedValue<int>(value), _bitrate);
    }

    public AnyEncodingVariant WithBitrate(Bitrate value) {
        return new AnyEncodingVariant(_height, _framesPerSecond, _width, new FixedValue<Bitrate>(value));
    }

    public AnyEncodingVariant WithBitrate(Func<AnyBitrate, AnyBitrate> configure) {
        return new AnyEncodingVariant(_height, _framesPerSecond, _width, configure(new AnyBitrate()));
    }

    public EncodingVariant Generate() {
        int height = _height.Generate();
        int fps    = _framesPerSecond.Generate();
        // Landscape only: the width is drawn from the height up, derived from the draw above it.
        int width = _width?.Generate() ?? Any.Int32().Between(Math.Max(128, height), 7680).MultipleOf(2).Generate();

        return new EncodingVariant($"{height}p{fps}", width, height, _bitrate.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
