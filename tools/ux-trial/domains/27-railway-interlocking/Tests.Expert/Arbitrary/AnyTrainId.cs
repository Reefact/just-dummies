// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrainId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TrainId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrainId : IAny<TrainId> {

    private readonly IAny<string> _headcode;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrainId()
        : this(headcode: AnyValidHeadcode()) { }

    private static AnyPattern AnyValidHeadcode() {
        // A headcode: a digit, a capital letter, two digits (e.g. 1A23).
        return Any.StringMatching("[0-9][A-Z][0-9]{2}");
    }

    private AnyTrainId(IAny<string> headcode) {
        _headcode = headcode;
    }

    /// <summary>Pins <c>headcode</c> to a fixed value.</summary>
    public AnyTrainId WithHeadcode(string value) {
        return new AnyTrainId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>headcode</c> from <c>AnyValidHeadcode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrainId WithHeadcode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyTrainId(configure(AnyValidHeadcode()));
    }

    /// <summary>Produces one arbitrary <see cref="TrainId" />.</summary>
    public TrainId Generate() {
        return new TrainId(_headcode.Generate());
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
