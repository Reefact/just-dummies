// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Individual --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Individual" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIndividual : IAny<Individual> {

    private readonly IAny<StudbookId>  _id;
    private readonly IAny<string>      _name;
    private readonly IAny<Sex>         _sex;
    private readonly IAny<DateOnly>    _birthDate;
    private readonly IAny<StudbookId?> _sireId;
    private readonly IAny<StudbookId?> _damId;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIndividual()
        : this(id:        AnyValidId(),
               name:      AnyValidName(),
               sex:       AnyValidSex(),
               birthDate: AnyValidBirthDate(),
               // Parentage is all or nothing, so the default is a founder; a test that needs
               // parents pins both through WithParents(sire, dam).
               sireId:    new FixedValue<StudbookId?>(null),
               damId:     new FixedValue<StudbookId?>(null)) { }

    private static AnyStudbookId AnyValidId() {
        return new AnyStudbookId();
    }

    private static AnyString AnyValidName() {
        return Any.String().Printable().NotBlank().WithMaxLength(40);
    }

    private static AnyEnum<Sex> AnyValidSex() {
        return Any.Enum<Sex>();
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly();
    }

    private static AnyStudbookId AnyValidSireId() {
        return new AnyStudbookId();
    }

    private static AnyStudbookId AnyValidDamId() {
        return new AnyStudbookId();
    }

    private AnyIndividual(IAny<StudbookId>  id,
                          IAny<string>      name,
                          IAny<Sex>         sex,
                          IAny<DateOnly>    birthDate,
                          IAny<StudbookId?> sireId,
                          IAny<StudbookId?> damId) {
        _id        = id;
        _name      = name;
        _sex       = sex;
        _birthDate = birthDate;
        _sireId    = sireId;
        _damId     = damId;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyIndividual WithId(StudbookId value) {
        return new AnyIndividual(new FixedValue<StudbookId>(value), _name, _sex, _birthDate, _sireId, _damId);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithId(Func<AnyStudbookId, AnyStudbookId> configure) {
        return new AnyIndividual(configure(AnyValidId()), _name, _sex, _birthDate, _sireId, _damId);
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyIndividual WithName(string value) {
        return new AnyIndividual(_id, new FixedValue<string>(value), _sex, _birthDate, _sireId, _damId);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithName(Func<AnyString, AnyString> configure) {
        return new AnyIndividual(_id, configure(AnyValidName()), _sex, _birthDate, _sireId, _damId);
    }

    /// <summary>Pins <c>sex</c> to a fixed value.</summary>
    public AnyIndividual WithSex(Sex value) {
        return new AnyIndividual(_id, _name, new FixedValue<Sex>(value), _birthDate, _sireId, _damId);
    }

    /// <summary>Draws <c>sex</c> from <c>AnyValidSex()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithSex(Func<AnyEnum<Sex>, AnyEnum<Sex>> configure) {
        return new AnyIndividual(_id, _name, configure(AnyValidSex()), _birthDate, _sireId, _damId);
    }

    /// <summary>Pins <c>birthDate</c> to a fixed value.</summary>
    public AnyIndividual WithBirthDate(DateOnly value) {
        return new AnyIndividual(_id, _name, _sex, new FixedValue<DateOnly>(value), _sireId, _damId);
    }

    /// <summary>Draws <c>birthDate</c> from <c>AnyValidBirthDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyIndividual(_id, _name, _sex, configure(AnyValidBirthDate()), _sireId, _damId);
    }

    /// <summary>Pins <c>sireId</c> to a fixed value.</summary>
    public AnyIndividual WithSireId(StudbookId? value) {
        return new AnyIndividual(_id, _name, _sex, _birthDate, new FixedValue<StudbookId?>(value), _damId);
    }

    /// <summary>Draws <c>sireId</c> from <c>AnyValidSireId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithSireId(Func<AnyStudbookId, AnyStudbookId> configure) {
        return new AnyIndividual(_id, _name, _sex, _birthDate, configure(AnyValidSireId()), _damId);
    }

    /// <summary>Pins <c>damId</c> to a fixed value.</summary>
    public AnyIndividual WithDamId(StudbookId? value) {
        return new AnyIndividual(_id, _name, _sex, _birthDate, _sireId, new FixedValue<StudbookId?>(value));
    }

    /// <summary>Draws <c>damId</c> from <c>AnyValidDamId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIndividual WithDamId(Func<AnyStudbookId, AnyStudbookId> configure) {
        return new AnyIndividual(_id, _name, _sex, _birthDate, _sireId, configure(AnyValidDamId()));
    }

    /// <summary>Produces one arbitrary <see cref="Individual" />.</summary>
    public Individual Generate() {
        return new Individual(_id.Generate(),
                              _name.Generate(),
                              _sex.Generate(),
                              _birthDate.Generate(),
                              _sireId.Generate(),
                              _damId.Generate());
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
