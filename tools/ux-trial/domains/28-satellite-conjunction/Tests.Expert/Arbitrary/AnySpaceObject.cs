// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpaceObject --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="SpaceObject" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpaceObject : IAny<SpaceObject> {

    private readonly IAny<int>        _catalogNumber;
    private readonly IAny<string>     _name;
    private readonly IAny<ObjectKind> _kind;
    private readonly IAny<double>     _hardBodyRadiusMeters;
    private readonly IAny<bool>       _isActive;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpaceObject()
        : this(catalogNumber:        AnyValidCatalogNumber(),
               name:                 AnyValidName(),
               kind:                 AnyValidKind(),
               hardBodyRadiusMeters: AnyValidHardBodyRadiusMeters(),
               isActive:             AnyValidIsActive()) { }

    private static AnyInt32 AnyValidCatalogNumber() {
        return Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber);
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(24);
    }

    private static AnyEnum<ObjectKind> AnyValidKind() {
        return Any.Enum<ObjectKind>();
    }

    private static AnyDouble AnyValidHardBodyRadiusMeters() {
        return Any.Double().GreaterThan(0).LessThanOrEqualTo(SpaceObject.MaximumHardBodyRadiusMeters);
    }

    private static AnyBoolean AnyValidIsActive() {
        // Only a payload may be active, a rule between two parameters: the default object is inert,
        // and Active() (AnySpaceObject.Custom.cs) sets the kind and the flag together.
        return Any.Boolean().False();
    }

    private AnySpaceObject(IAny<int>        catalogNumber,
                           IAny<string>     name,
                           IAny<ObjectKind> kind,
                           IAny<double>     hardBodyRadiusMeters,
                           IAny<bool>       isActive) {
        _catalogNumber        = catalogNumber;
        _name                 = name;
        _kind                 = kind;
        _hardBodyRadiusMeters = hardBodyRadiusMeters;
        _isActive             = isActive;
    }

    /// <summary>Pins <c>catalogNumber</c> to a fixed value.</summary>
    public AnySpaceObject WithCatalogNumber(int value) {
        return new AnySpaceObject(new FixedValue<int>(value), _name, _kind, _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Draws <c>catalogNumber</c> from <c>AnyValidCatalogNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpaceObject WithCatalogNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnySpaceObject(configure(AnyValidCatalogNumber()), _name, _kind, _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnySpaceObject WithName(string value) {
        return new AnySpaceObject(_catalogNumber, new FixedValue<string>(value), _kind, _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpaceObject WithName(Func<AnyString, AnyString> configure) {
        return new AnySpaceObject(_catalogNumber, configure(AnyValidName()), _kind, _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnySpaceObject WithKind(ObjectKind value) {
        return new AnySpaceObject(_catalogNumber, _name, new FixedValue<ObjectKind>(value), _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpaceObject WithKind(Func<AnyEnum<ObjectKind>, AnyEnum<ObjectKind>> configure) {
        return new AnySpaceObject(_catalogNumber, _name, configure(AnyValidKind()), _hardBodyRadiusMeters, _isActive);
    }

    /// <summary>Pins <c>hardBodyRadiusMeters</c> to a fixed value.</summary>
    public AnySpaceObject WithHardBodyRadiusMeters(double value) {
        return new AnySpaceObject(_catalogNumber, _name, _kind, new FixedValue<double>(value), _isActive);
    }

    /// <summary>Draws <c>hardBodyRadiusMeters</c> from <c>AnyValidHardBodyRadiusMeters()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpaceObject WithHardBodyRadiusMeters(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpaceObject(_catalogNumber, _name, _kind, configure(AnyValidHardBodyRadiusMeters()), _isActive);
    }

    /// <summary>Pins <c>isActive</c> to a fixed value.</summary>
    public AnySpaceObject WithIsActive(bool value) {
        return new AnySpaceObject(_catalogNumber, _name, _kind, _hardBodyRadiusMeters, new FixedValue<bool>(value));
    }

    /// <summary>Draws <c>isActive</c> from <c>AnyValidIsActive()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpaceObject WithIsActive(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnySpaceObject(_catalogNumber, _name, _kind, _hardBodyRadiusMeters, configure(AnyValidIsActive()));
    }

    /// <summary>Produces one arbitrary <see cref="SpaceObject" />.</summary>
    public SpaceObject Generate() {
        return new SpaceObject(_catalogNumber.Generate(),
                               _name.Generate(),
                               _kind.Generate(),
                               _hardBodyRadiusMeters.Generate(),
                               _isActive.Generate());
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
