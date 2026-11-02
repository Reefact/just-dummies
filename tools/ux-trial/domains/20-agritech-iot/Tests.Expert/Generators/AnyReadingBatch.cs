// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReadingBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using AgriTech.Domain;
using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ReadingBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReadingBatch : IAny<ReadingBatch> {

    private IAny<SensorId>                     _gateway;
    private IAny<IReadOnlyList<SensorReading>> _readings;

    private AnySensorId            _gatewayRecipe;
    private AnyList<SensorReading> _readingsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReadingBatch()
        : this(gateway:  AnyValidGateway(),
               readings: AnyValidReadings()) { }

    private static AnySensorId AnyValidGateway() {
        return new AnySensorId();
    }

    private static AnyList<SensorReading> AnyValidReadings() {
        // A handful of readings is an ordinary batch; the 500 000 ceiling is for the one test that is about it.
        return Any.ListOf(new AnySensorReading()).WithCountBetween(1, 20);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReadingBatch(AnySensorId            gateway,
                            AnyList<SensorReading> readings) {
        _gateway        = gateway;
        _gatewayRecipe  = gateway;
        _readings       = readings;
        _readingsRecipe = readings;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReadingBatch(AnyReadingBatch from) {
        _gateway        = from._gateway;
        _gatewayRecipe  = from._gatewayRecipe;
        _readings       = from._readings;
        _readingsRecipe = from._readingsRecipe;
    }

    /// <summary>Pins <c>gateway</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReadingBatch WithGateway(SensorId value) {
        AnyReadingBatch copy = new AnyReadingBatch(this);

        copy._gateway = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gateway</c> is drawn from — <c>AnyValidGateway()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySensorId, AnySensorId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReadingBatch WithGateway(Func<AnySensorId, AnySensorId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReadingBatch copy = new AnyReadingBatch(this);

        copy._gatewayRecipe = configure(_gatewayRecipe);
        copy._gateway       = copy._gatewayRecipe;

        return copy;
    }

    /// <summary>Pins <c>readings</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReadingBatch WithReadings(IReadOnlyList<SensorReading> value) {
        AnyReadingBatch copy = new AnyReadingBatch(this);

        copy._readings = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>readings</c> is drawn from — <c>AnyValidReadings()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;SensorReading&gt;, AnyList&lt;SensorReading&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReadingBatch WithReadings(Func<AnyList<SensorReading>, AnyList<SensorReading>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReadingBatch copy = new AnyReadingBatch(this);

        copy._readingsRecipe = configure(_readingsRecipe);
        copy._readings       = copy._readingsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReadingBatch" />.</summary>
    public ReadingBatch Generate() {
        return new ReadingBatch(_gateway.Generate(),
                                _readings.Generate());
    }

}
