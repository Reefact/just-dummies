// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoomNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RoomNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoomNumber : IAny<RoomNumber> {

    private IAny<string> _value;

    private AnyPattern _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoomNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("[1-9][0-9]?[0-9]{2}[A-D]?");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoomNumber(AnyPattern value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoomNumber(AnyRoomNumber from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoomNumber WithValue(string value) {
        AnyRoomNumber copy = new AnyRoomNumber(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoomNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoomNumber copy = new AnyRoomNumber(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RoomNumber" />.</summary>
    public RoomNumber Generate() {
        return new RoomNumber(_value.Generate());
    }

}
