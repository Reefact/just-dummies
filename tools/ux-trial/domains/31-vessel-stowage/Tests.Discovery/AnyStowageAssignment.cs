// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StowageAssignment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StowageAssignment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStowageAssignment : IAny<StowageAssignment> {

    private IAny<Container>    _container;
    private IAny<SlotPosition> _slot;

    private bool            _containerPinned;
    private bool            _slotPinned;
    private AnyContainer    _containerRecipe;
    private AnySlotPosition _slotRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStowageAssignment()
        : this(container: AnyValidContainer(),
               slot:      AnyValidSlot()) { }

    private static AnyContainer AnyValidContainer() {
        return new AnyContainer();
    }

    private static AnySlotPosition AnyValidSlot() {
        return new AnySlotPosition();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStowageAssignment(AnyContainer    container,
                                 AnySlotPosition slot) {
        _container       = container;
        _containerRecipe = container;
        _slot            = slot;
        _slotRecipe      = slot;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStowageAssignment(AnyStowageAssignment from) {
        _container       = from._container;
        _containerRecipe = from._containerRecipe;
        _slot            = from._slot;
        _slotRecipe      = from._slotRecipe;
        _containerPinned = from._containerPinned;
        _slotPinned      = from._slotPinned;
    }

    /// <summary>Pins <c>container</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStowageAssignment WithContainer(Container value) {
        AnyStowageAssignment copy = new AnyStowageAssignment(this);

        copy._container       = Any.Fixed(value);
        copy._containerPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>container</c> is drawn from — <c>AnyValidContainer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyContainer, AnyContainer&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStowageAssignment WithContainer(Func<AnyContainer, AnyContainer> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStowageAssignment copy = new AnyStowageAssignment(this);

        copy._containerRecipe = configure(_containerRecipe);
        copy._container       = copy._containerRecipe;
        copy._containerPinned = false;

        return copy;
    }

    /// <summary>Pins <c>slot</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStowageAssignment WithSlot(SlotPosition value) {
        AnyStowageAssignment copy = new AnyStowageAssignment(this);

        copy._slot       = Any.Fixed(value);
        copy._slotPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>slot</c> is drawn from — <c>AnyValidSlot()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySlotPosition, AnySlotPosition&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStowageAssignment WithSlot(Func<AnySlotPosition, AnySlotPosition> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStowageAssignment copy = new AnyStowageAssignment(this);

        copy._slotRecipe = configure(_slotRecipe);
        copy._slot       = copy._slotRecipe;
        copy._slotPinned = false;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StowageAssignment" />.</summary>
    public StowageAssignment Generate() {
        // The slot's bay must accept the container's size: whichever side the test left open follows the other.
        if (_containerPinned && !_slotPinned) {
            Container container = _container.Generate();
            SlotPosition slot = _slotRecipe.WithBay(bay => container.Size == ContainerSize.TwentyFoot ? bay.Odd() : bay.Even()).Generate();

            return new StowageAssignment(container, slot);
        } else {
            SlotPosition slot = _slot.Generate();
            Container container = _containerPinned ? _container.Generate() : _containerRecipe.WithSize(slot.AcceptedSize).Generate();

            return new StowageAssignment(container, slot);
        }
    }

}
