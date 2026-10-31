// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TranslationTransform --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="TranslationTransform" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTranslationTransform : IAny<TranslationTransform> {

    private readonly IAny<double> _dxMm;
    private readonly IAny<double> _dyMm;
    private readonly IAny<double> _dzMm;
    private readonly IAny<double> _residualErrorMm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTranslationTransform()
        : this(dxMm:            AnyValidDxMm(),
               dyMm:            AnyValidDyMm(),
               dzMm:            AnyValidDzMm(),
               residualErrorMm: AnyValidResidualErrorMm()) { }

    private static AnyDouble AnyValidDxMm() {
        return Any.Double().Between(-TranslationTransform.MaxShiftMm, TranslationTransform.MaxShiftMm);
    }

    private static AnyDouble AnyValidDyMm() {
        return Any.Double().Between(-TranslationTransform.MaxShiftMm, TranslationTransform.MaxShiftMm);
    }

    private static AnyDouble AnyValidDzMm() {
        return Any.Double().Between(-TranslationTransform.MaxShiftMm, TranslationTransform.MaxShiftMm);
    }

    private static AnyDouble AnyValidResidualErrorMm() {
        return Any.Double().Between(0.0, TranslationTransform.MaxResidualErrorMm);
    }

    private AnyTranslationTransform(IAny<double> dxMm,
                                    IAny<double> dyMm,
                                    IAny<double> dzMm,
                                    IAny<double> residualErrorMm) {
        _dxMm            = dxMm;
        _dyMm            = dyMm;
        _dzMm            = dzMm;
        _residualErrorMm = residualErrorMm;
    }

    /// <summary>Pins <c>dxMm</c> to a fixed value.</summary>
    public AnyTranslationTransform WithDxMm(double value) {
        return new AnyTranslationTransform(new FixedValue<double>(value), _dyMm, _dzMm, _residualErrorMm);
    }

    /// <summary>Draws <c>dxMm</c> from <c>AnyValidDxMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTranslationTransform WithDxMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTranslationTransform(configure(AnyValidDxMm()), _dyMm, _dzMm, _residualErrorMm);
    }

    /// <summary>Pins <c>dyMm</c> to a fixed value.</summary>
    public AnyTranslationTransform WithDyMm(double value) {
        return new AnyTranslationTransform(_dxMm, new FixedValue<double>(value), _dzMm, _residualErrorMm);
    }

    /// <summary>Draws <c>dyMm</c> from <c>AnyValidDyMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTranslationTransform WithDyMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTranslationTransform(_dxMm, configure(AnyValidDyMm()), _dzMm, _residualErrorMm);
    }

    /// <summary>Pins <c>dzMm</c> to a fixed value.</summary>
    public AnyTranslationTransform WithDzMm(double value) {
        return new AnyTranslationTransform(_dxMm, _dyMm, new FixedValue<double>(value), _residualErrorMm);
    }

    /// <summary>Draws <c>dzMm</c> from <c>AnyValidDzMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTranslationTransform WithDzMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTranslationTransform(_dxMm, _dyMm, configure(AnyValidDzMm()), _residualErrorMm);
    }

    /// <summary>Pins <c>residualErrorMm</c> to a fixed value.</summary>
    public AnyTranslationTransform WithResidualErrorMm(double value) {
        return new AnyTranslationTransform(_dxMm, _dyMm, _dzMm, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>residualErrorMm</c> from <c>AnyValidResidualErrorMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTranslationTransform WithResidualErrorMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTranslationTransform(_dxMm, _dyMm, _dzMm, configure(AnyValidResidualErrorMm()));
    }

    /// <summary>Produces one arbitrary <see cref="TranslationTransform" />.</summary>
    public TranslationTransform Generate() {
        return new TranslationTransform(_dxMm.Generate(),
                                        _dyMm.Generate(),
                                        _dzMm.Generate(),
                                        _residualErrorMm.Generate());
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
