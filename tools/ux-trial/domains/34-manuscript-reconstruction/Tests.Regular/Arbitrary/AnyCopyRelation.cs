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

    private static IAny<WitnessSiglum> AnyValidExemplar() {
        // exemplar != copy: the exemplar's siglum starts in A-M, the copy's in N-Z.
        return Any.StringMatching("[A-M][A-Za-z0-9.]{0,11}").As(value => new WitnessSiglum(value));
    }

    private static IAny<WitnessSiglum> AnyValidCopy() {
        // exemplar != copy: the exemplar's siglum starts in A-M, the copy's in N-Z.
        return Any.StringMatching("[N-Z][A-Za-z0-9.]{0,11}").As(value => new WitnessSiglum(value));
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

    /// <summary>Pins <c>copy</c> to a fixed value.</summary>
    public AnyCopyRelation WithCopy(WitnessSiglum value) {
        return new AnyCopyRelation(_exemplar, new FixedValue<WitnessSiglum>(value), _confidence, _sharedErrorCount);
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
        return new CopyRelation(_exemplar.Generate(),
                                _copy.Generate(),
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
