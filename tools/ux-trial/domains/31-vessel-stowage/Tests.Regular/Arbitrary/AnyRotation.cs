// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Rotation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<IEnumerable<PortCode>> _ports;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRotation()
        : this(ports: AnyValidPorts()) { }

    private static AnySequence<PortCode> AnyValidPorts() {
        // A rotation calls at least two ports, each once.
        return Any.SequenceOf(new AnyPortCode()).WithCountBetween(2, 8).Distinct();
    }

    private AnyRotation(IAny<IEnumerable<PortCode>> ports) {
        _ports = ports;
    }

    /// <summary>Pins <c>ports</c> to a fixed value.</summary>
    public AnyRotation WithPorts(IEnumerable<PortCode> value) {
        return new AnyRotation(new FixedValue<IEnumerable<PortCode>>(value));
    }

    /// <summary>Draws <c>ports</c> from <c>AnyValidPorts()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRotation WithPorts(Func<AnySequence<PortCode>, AnySequence<PortCode>> configure) {
        return new AnyRotation(configure(AnyValidPorts()));
    }

    /// <summary>Produces one arbitrary <see cref="Rotation" />.</summary>
    public Rotation Generate() {
        return new Rotation(_ports.Generate());
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
