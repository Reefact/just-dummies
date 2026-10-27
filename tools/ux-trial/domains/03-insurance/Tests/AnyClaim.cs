// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Claim --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Claim" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClaim : IAny<Claim> {

    private readonly IAny<Guid>           _id;
    private readonly IAny<DateOnly>       _occurredOn;
    private readonly IAny<decimal>        _damage;
    private readonly IAny<Responsibility> _responsibility;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyClaim()
        : this(id:             AnyValidId(),
               occurredOn:     AnyValidOccurredOn(),
               damage:         AnyValidDamage(),
               responsibility: AnyValidResponsibility()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyDateOnly AnyValidOccurredOn() {
        return Any.DateOnly();
    }

    private static AnyDecimal AnyValidDamage() {
        return Any.Decimal().Positive();
    }

    private static AnyEnum<Responsibility> AnyValidResponsibility() {
        return Any.Enum<Responsibility>();
    }

    private AnyClaim(IAny<Guid>           id,
                     IAny<DateOnly>       occurredOn,
                     IAny<decimal>        damage,
                     IAny<Responsibility> responsibility) {
        _id             = id;
        _occurredOn     = occurredOn;
        _damage         = damage;
        _responsibility = responsibility;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyClaim WithId(Guid value) {
        return new AnyClaim(new FixedValue<Guid>(value), _occurredOn, _damage, _responsibility);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClaim WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyClaim(configure(AnyValidId()), _occurredOn, _damage, _responsibility);
    }

    /// <summary>Pins <c>occurredOn</c> to a fixed value.</summary>
    public AnyClaim WithOccurredOn(DateOnly value) {
        return new AnyClaim(_id, new FixedValue<DateOnly>(value), _damage, _responsibility);
    }

    /// <summary>Draws <c>occurredOn</c> from <c>AnyValidOccurredOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClaim WithOccurredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyClaim(_id, configure(AnyValidOccurredOn()), _damage, _responsibility);
    }

    /// <summary>Pins <c>damage</c> to a fixed value.</summary>
    public AnyClaim WithDamage(decimal value) {
        return new AnyClaim(_id, _occurredOn, new FixedValue<decimal>(value), _responsibility);
    }

    /// <summary>Draws <c>damage</c> from <c>AnyValidDamage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClaim WithDamage(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyClaim(_id, _occurredOn, configure(AnyValidDamage()), _responsibility);
    }

    /// <summary>Pins <c>responsibility</c> to a fixed value.</summary>
    public AnyClaim WithResponsibility(Responsibility value) {
        return new AnyClaim(_id, _occurredOn, _damage, new FixedValue<Responsibility>(value));
    }

    /// <summary>Draws <c>responsibility</c> from <c>AnyValidResponsibility()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClaim WithResponsibility(Func<AnyEnum<Responsibility>, AnyEnum<Responsibility>> configure) {
        return new AnyClaim(_id, _occurredOn, _damage, configure(AnyValidResponsibility()));
    }

    /// <summary>Produces one arbitrary <see cref="Claim" />.</summary>
    public Claim Generate() {
        return new Claim(_id.Generate(),
                         _occurredOn.Generate(),
                         _damage.Generate(),
                         _responsibility.Generate());
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
