// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EncodingVariant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="EncodingVariant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEncodingVariant : IAny<EncodingVariant> {

    private readonly IAny<int>     _framesPerSecond;
    private readonly IAny<int>     _width;
    private readonly IAny<int>     _height;
    private readonly IAny<Bitrate> _bitrate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEncodingVariant()
        : this(framesPerSecond: AnyValidFramesPerSecond(),
               width:           AnyValidWidth(),
               height:          AnyValidHeight(),
               bitrate:         AnyValidBitrate()) { }

    private static AnyInt32 AnyValidFramesPerSecond() {
        // The id is "<height>p<fps>": its height is the drawn height (see Generate), so only the frame rate is drawn here.
        return Any.Int32().OneOf(24, 25, 30, 50, 60);
    }

    private static AnyInt32 AnyValidWidth() {
        // At least the tallest height, so height <= width holds by construction.
        return Any.Int32().Between(4320, 7680).MultipleOf(2);
    }

    private static AnyInt32 AnyValidHeight() {
        // From 100, not the 96 the height guard allows: the id spells the height in three or four digits.
        return Any.Int32().Between(100, 4320).MultipleOf(2);
    }

    private static AnyBitrate AnyValidBitrate() {
        return new AnyBitrate();
    }

    private AnyEncodingVariant(IAny<int>     framesPerSecond,
                               IAny<int>     width,
                               IAny<int>     height,
                               IAny<Bitrate> bitrate) {
        _framesPerSecond = framesPerSecond;
        _width   = width;
        _height  = height;
        _bitrate = bitrate;
    }

    /// <summary>Pins the frame rate the id declares.</summary>
    public AnyEncodingVariant WithFramesPerSecond(int value) {
        return new AnyEncodingVariant(new FixedValue<int>(value), _width, _height, _bitrate);
    }

    /// <summary>Pins <c>width</c> to a fixed value.</summary>
    public AnyEncodingVariant WithWidth(int value) {
        return new AnyEncodingVariant(_framesPerSecond, new FixedValue<int>(value), _height, _bitrate);
    }

    /// <summary>Draws <c>width</c> from <c>AnyValidWidth()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithWidth(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEncodingVariant(_framesPerSecond, configure(AnyValidWidth()), _height, _bitrate);
    }

    /// <summary>Pins <c>height</c> to a fixed value.</summary>
    public AnyEncodingVariant WithHeight(int value) {
        return new AnyEncodingVariant(_framesPerSecond, _width, new FixedValue<int>(value), _bitrate);
    }

    /// <summary>Draws <c>height</c> from <c>AnyValidHeight()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithHeight(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEncodingVariant(_framesPerSecond, _width, configure(AnyValidHeight()), _bitrate);
    }

    /// <summary>Pins <c>bitrate</c> to a fixed value.</summary>
    public AnyEncodingVariant WithBitrate(Bitrate value) {
        return new AnyEncodingVariant(_framesPerSecond, _width, _height, new FixedValue<Bitrate>(value));
    }

    /// <summary>Draws <c>bitrate</c> from <c>AnyValidBitrate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithBitrate(Func<AnyBitrate, AnyBitrate> configure) {
        return new AnyEncodingVariant(_framesPerSecond, _width, _height, configure(AnyValidBitrate()));
    }

    /// <summary>Produces one arbitrary <see cref="EncodingVariant" />.</summary>
    public EncodingVariant Generate() {
        int height = _height.Generate();
        string id = $"{height}p{_framesPerSecond.Generate()}";

        return new EncodingVariant(id, _width.Generate(), height, _bitrate.Generate());
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
