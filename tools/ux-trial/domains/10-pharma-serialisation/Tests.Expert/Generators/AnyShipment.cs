// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Shipment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShipment : IAny<Shipment> {

    private IAny<string>                      _reference;
    private IAny<IReadOnlyList<SerialNumber>> _serials;
    private IAny<DateOnly>                    _shippedOn;

    private AnyList<SerialNumber> _serialsRecipe;
    private AnyDateOnly           _shippedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShipment()
        : this(reference: AnyValidReference(),
               serials:   AnyValidSerials(),
               shippedOn: AnyValidShippedOn()) { }

    private static IAny<string> AnyValidReference() {
        return new AnyShipmentReference();
    }

    private static AnyList<SerialNumber> AnyValidSerials() {
        return Any.ListOf(new AnySerialNumber()).NonEmpty().Distinct();
    }

    private static AnyDateOnly AnyValidShippedOn() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShipment(IAny<string>          reference,
                        AnyList<SerialNumber> serials,
                        AnyDateOnly           shippedOn) {
        _reference       = reference;
        _serials         = serials;
        _serialsRecipe   = serials;
        _shippedOn       = shippedOn;
        _shippedOnRecipe = shippedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShipment(AnyShipment from) {
        _reference       = from._reference;
        _serials         = from._serials;
        _serialsRecipe   = from._serialsRecipe;
        _shippedOn       = from._shippedOn;
        _shippedOnRecipe = from._shippedOnRecipe;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithReference(string value) {
        AnyShipment copy = new AnyShipment(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>serials</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithSerials(IReadOnlyList<SerialNumber> value) {
        AnyShipment copy = new AnyShipment(this);

        copy._serials = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>serials</c> is drawn from — <c>AnyValidSerials()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;SerialNumber&gt;, AnyList&lt;SerialNumber&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithSerials(Func<AnyList<SerialNumber>, AnyList<SerialNumber>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._serialsRecipe = configure(_serialsRecipe);
        copy._serials       = copy._serialsRecipe;

        return copy;
    }

    /// <summary>Pins <c>shippedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithShippedOn(DateOnly value) {
        AnyShipment copy = new AnyShipment(this);

        copy._shippedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shippedOn</c> is drawn from — <c>AnyValidShippedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithShippedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._shippedOnRecipe = configure(_shippedOnRecipe);
        copy._shippedOn       = copy._shippedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Shipment" />.</summary>
    public Shipment Generate() {
        return new Shipment(_reference.Generate(),
                            _serials.Generate(),
                            _shippedOn.Generate());
    }

}
