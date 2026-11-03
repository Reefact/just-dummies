// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Rotation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Rotation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRotation : IAny<Rotation> {

    private IAny<IEnumerable<PortCode>> _ports;

    private AnySequence<PortCode> _portsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRotation()
        : this(ports: AnyValidPorts()) { }

    private static AnySequence<PortCode> AnyValidPorts() {

        return Any.SequenceOf(new AnyPortCode()).WithCountBetween(2, 6).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRotation(AnySequence<PortCode> ports) {
        _ports       = ports;
        _portsRecipe = ports;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRotation(AnyRotation from) {
        _ports       = from._ports;
        _portsRecipe = from._portsRecipe;
    }

    /// <summary>Pins <c>ports</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRotation WithPorts(IEnumerable<PortCode> value) {
        AnyRotation copy = new AnyRotation(this);

        copy._ports = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ports</c> is drawn from — <c>AnyValidPorts()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;PortCode&gt;, AnySequence&lt;PortCode&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRotation WithPorts(Func<AnySequence<PortCode>, AnySequence<PortCode>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRotation copy = new AnyRotation(this);

        copy._portsRecipe = configure(_portsRecipe);
        copy._ports       = copy._portsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Rotation" />.</summary>
    public Rotation Generate() {
        return new Rotation(_ports.Generate());
    }

}
