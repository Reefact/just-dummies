// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchRun --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StitchRun" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStitchRun : IAny<StitchRun> {

    private readonly IAny<StitchKind> _kind;
    private readonly IAny<int>        _count;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStitchRun()
        : this(kind:  AnyValidKind(),
               count: AnyValidCount()) { }

    private static AnyEnum<StitchKind> AnyValidKind() {
        return Any.Enum<StitchKind>();
    }

    private static AnyInt32 AnyValidCount() {
        return Any.Int32().Between(1, StitchRun.MaxCount);
    }

    private AnyStitchRun(IAny<StitchKind> kind,
                         IAny<int>        count) {
        _kind  = kind;
        _count = count;
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyStitchRun WithKind(StitchKind value) {
        return new AnyStitchRun(new FixedValue<StitchKind>(value), _count);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStitchRun WithKind(Func<AnyEnum<StitchKind>, AnyEnum<StitchKind>> configure) {
        return new AnyStitchRun(configure(AnyValidKind()), _count);
    }

    /// <summary>Pins <c>count</c> to a fixed value.</summary>
    public AnyStitchRun WithCount(int value) {
        return new AnyStitchRun(_kind, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>count</c> from <c>AnyValidCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStitchRun WithCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyStitchRun(_kind, configure(AnyValidCount()));
    }

    /// <summary>Produces one arbitrary <see cref="StitchRun" />.</summary>
    public StitchRun Generate() {
        return new StitchRun(_kind.Generate(),
                             _count.Generate());
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
