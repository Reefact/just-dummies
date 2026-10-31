// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CopyRelation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CopyRelation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCopyRelation : IAny<CopyRelation> {

    private readonly IAny<WitnessSiglum> _exemplar;
    private readonly IAny<WitnessSiglum> _copy;
    private readonly IAny<double>        _confidence;
    private readonly IAny<int>           _sharedErrorCount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCopyRelation()
        : this(exemplar:         AnyValidExemplar(),
               copy:             AnyValidCopy(),
               confidence:       AnyValidConfidence(),
               sharedErrorCount: AnyValidSharedErrorCount()) { }

    private static AnyWitnessSiglum AnyValidExemplar() {
        return new AnyWitnessSiglum();
    }

    private static AnyWitnessSiglum AnyValidCopy() {
        return new AnyWitnessSiglum();
    }

    private static AnyDouble AnyValidConfidence() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(1.0);
    }

    private static AnyInt32 AnyValidSharedErrorCount() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private AnyCopyRelation(IAny<WitnessSiglum> exemplar,
                            IAny<WitnessSiglum> copy,
                            IAny<double>        confidence,
                            IAny<int>           sharedErrorCount) {
        _exemplar         = exemplar;
        _copy             = copy;
        _confidence       = confidence;
        _sharedErrorCount = sharedErrorCount;
    }

    /// <summary>Pins <c>exemplar</c> to a fixed value.</summary>
    public AnyCopyRelation WithExemplar(WitnessSiglum value) {
        return new AnyCopyRelation(new FixedValue<WitnessSiglum>(value), _copy, _confidence, _sharedErrorCount);
    }

    /// <summary>Draws <c>exemplar</c> from <c>AnyValidExemplar()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCopyRelation WithExemplar(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        return new AnyCopyRelation(configure(AnyValidExemplar()), _copy, _confidence, _sharedErrorCount);
    }

    /// <summary>Pins <c>copy</c> to a fixed value.</summary>
    public AnyCopyRelation WithCopy(WitnessSiglum value) {
        return new AnyCopyRelation(_exemplar, new FixedValue<WitnessSiglum>(value), _confidence, _sharedErrorCount);
    }

    /// <summary>Draws <c>copy</c> from <c>AnyValidCopy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCopyRelation WithCopy(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        return new AnyCopyRelation(_exemplar, configure(AnyValidCopy()), _confidence, _sharedErrorCount);
    }

    /// <summary>Pins <c>confidence</c> to a fixed value.</summary>
    public AnyCopyRelation WithConfidence(double value) {
        return new AnyCopyRelation(_exemplar, _copy, new FixedValue<double>(value), _sharedErrorCount);
    }

    /// <summary>Draws <c>confidence</c> from <c>AnyValidConfidence()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCopyRelation WithConfidence(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCopyRelation(_exemplar, _copy, configure(AnyValidConfidence()), _sharedErrorCount);
    }

    /// <summary>Pins <c>sharedErrorCount</c> to a fixed value.</summary>
    public AnyCopyRelation WithSharedErrorCount(int value) {
        return new AnyCopyRelation(_exemplar, _copy, _confidence, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>sharedErrorCount</c> from <c>AnyValidSharedErrorCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCopyRelation WithSharedErrorCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCopyRelation(_exemplar, _copy, _confidence, configure(AnyValidSharedErrorCount()));
    }

    /// <summary>Produces one arbitrary <see cref="CopyRelation" />.</summary>
    public CopyRelation Generate() {
        // A witness is never a copy of itself: a drawn copy is told which exemplar to differ from.
        WitnessSiglum exemplar = _exemplar.Generate();
        WitnessSiglum copy     = _copy is AnyWitnessSiglum anyCopy
                                     ? anyCopy.WithValue(v => v.DifferentFrom(exemplar.Value)).Generate()
                                     : _copy.Generate();

        return new CopyRelation(exemplar,
                                copy,
                                _confidence.Generate(),
                                _sharedErrorCount.Generate());
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
