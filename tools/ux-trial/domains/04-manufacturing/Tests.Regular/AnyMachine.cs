// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Machine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Machine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMachine : IAny<Machine> {

    private IAny<string>       _machineId;
    private IAny<MachineState> _initialState;

    private AnyPattern             _machineIdRecipe;
    private AnyEnum<MachineState> _initialStateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMachine()
        : this(machineId:    AnyValidMachineId(),
               initialState: AnyValidInitialState()) { }

    private static AnyPattern AnyValidMachineId() {
        return Any.StringMatching("M-[0-9]{3}");
    }

    private static AnyEnum<MachineState> AnyValidInitialState() {
        return Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMachine(AnyPattern             machineId,
                       AnyEnum<MachineState> initialState) {
        _machineId          = machineId;
        _machineIdRecipe    = machineId;
        _initialState       = initialState;
        _initialStateRecipe = initialState;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMachine(AnyMachine from) {
        _machineId          = from._machineId;
        _machineIdRecipe    = from._machineIdRecipe;
        _initialState       = from._initialState;
        _initialStateRecipe = from._initialStateRecipe;
    }

    /// <summary>Pins <c>machineId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMachine WithMachineId(string value) {
        AnyMachine copy = new AnyMachine(this);

        copy._machineId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>machineId</c> is drawn from — <c>AnyValidMachineId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMachine WithMachineId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMachine copy = new AnyMachine(this);

        copy._machineIdRecipe = configure(_machineIdRecipe);
        copy._machineId       = copy._machineIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>initialState</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMachine WithInitialState(MachineState value) {
        AnyMachine copy = new AnyMachine(this);

        copy._initialState = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>initialState</c> is drawn from — <c>AnyValidInitialState()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;MachineState&gt;, AnyEnum&lt;MachineState&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMachine WithInitialState(Func<AnyEnum<MachineState>, AnyEnum<MachineState>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMachine copy = new AnyMachine(this);

        copy._initialStateRecipe = configure(_initialStateRecipe);
        copy._initialState       = copy._initialStateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Machine" />.</summary>
    public Machine Generate() {
        return new Machine(_machineId.Generate(),
                           _initialState.Generate());
    }

}
