// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EncodingVariant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EncodingVariant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEncodingVariant : IAny<EncodingVariant> {

    private readonly IAny<string>  _id;
    private readonly IAny<int>     _width;
    private readonly IAny<int>     _height;
    private readonly IAny<Bitrate> _bitrate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEncodingVariant()
        : this(id:      AnyValidId(),
               width:   AnyValidWidth(),
               height:  AnyValidHeight(),
               bitrate: AnyValidBitrate()) { }

    private static AnyString AnyValidId() {
        // The id is '<height>p<fps>' and must agree with the height, so Generate() derives it from the
        // drawn height; this base only draws the frame-rate suffix.
        return Any.String().OneOf("24", "25", "30", "50", "60");
    }

    private static AnyInt32 AnyValidWidth() {
        // Even, and never below the height (landscape only): the base starts above the tallest height.
        return Any.Int32().Between(2160, 7680).MultipleOf(2);
    }

    private static AnyInt32 AnyValidHeight() {
        // The usual rendition heights: all even, all within 96..4320.
        return Any.Int32().OneOf(144, 240, 360, 480, 720, 1080, 1440, 2160);
    }

    private static AnyBitrate AnyValidBitrate() {
        return new AnyBitrate();
    }

    private AnyEncodingVariant(IAny<string>  id,
                               IAny<int>     width,
                               IAny<int>     height,
                               IAny<Bitrate> bitrate) {
        _id      = id;
        _width   = width;
        _height  = height;
        _bitrate = bitrate;
    }

    /// <summary>Pins the frame rate the id ends with.</summary>
    public AnyEncodingVariant WithFramesPerSecond(int value) {
        return new AnyEncodingVariant(new FixedValue<string>(value.ToString(System.Globalization.CultureInfo.InvariantCulture)), _width, _height, _bitrate);
    }

    /// <summary>Pins <c>width</c> to a fixed value.</summary>
    public AnyEncodingVariant WithWidth(int value) {
        return new AnyEncodingVariant(_id, new FixedValue<int>(value), _height, _bitrate);
    }

    /// <summary>Draws <c>width</c> from <c>AnyValidWidth()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithWidth(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEncodingVariant(_id, configure(AnyValidWidth()), _height, _bitrate);
    }

    /// <summary>Pins <c>height</c> to a fixed value.</summary>
    public AnyEncodingVariant WithHeight(int value) {
        return new AnyEncodingVariant(_id, _width, new FixedValue<int>(value), _bitrate);
    }

    /// <summary>Draws <c>height</c> from <c>AnyValidHeight()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithHeight(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEncodingVariant(_id, _width, configure(AnyValidHeight()), _bitrate);
    }

    /// <summary>Pins <c>bitrate</c> to a fixed value.</summary>
    public AnyEncodingVariant WithBitrate(Bitrate value) {
        return new AnyEncodingVariant(_id, _width, _height, new FixedValue<Bitrate>(value));
    }

    /// <summary>Draws <c>bitrate</c> from <c>AnyValidBitrate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithBitrate(Func<AnyBitrate, AnyBitrate> configure) {
        return new AnyEncodingVariant(_id, _width, _height, configure(AnyValidBitrate()));
    }

    /// <summary>Produces one arbitrary <see cref="EncodingVariant" />.</summary>
    public EncodingVariant Generate() {
        int height = _height.Generate();

        return new EncodingVariant($"{height}p{_id.Generate()}",
                                   _width.Generate(),
                                   height,
                                   _bitrate.Generate());
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
