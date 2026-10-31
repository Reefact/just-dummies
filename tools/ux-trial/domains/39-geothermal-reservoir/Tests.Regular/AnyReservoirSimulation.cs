// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReservoirSimulation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReservoirSimulation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReservoirSimulation : IAny<ReservoirSimulation> {

    private readonly IAny<GeologicalGrid>            _grid;
    private readonly IAny<IReadOnlyCollection<Well>> _wells;
    private readonly IAny<BoundaryCondition>         _boundary;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReservoirSimulation()
        : this(grid:     AnyValidGrid(),
               wells:    AnyValidWells(),
               boundary: AnyValidBoundary()) { }

    private static AnyGeologicalGrid AnyValidGrid() {
        return new AnyGeologicalGrid();
    }

    private static AnyList<Well> AnyValidWells() {
        // Wells must be completed in distinct cells of *this* grid, which a per-parameter recipe cannot
        // know: by default the simulation has none, and a test pins the wells it is about.
        return Any.ListOf(new AnyWell()).Empty();
    }

    private static AnyBoundaryCondition AnyValidBoundary() {
        return new AnyBoundaryCondition();
    }

    private AnyReservoirSimulation(IAny<GeologicalGrid>            grid,
                                   IAny<IReadOnlyCollection<Well>> wells,
                                   IAny<BoundaryCondition>         boundary) {
        _grid     = grid;
        _wells    = wells;
        _boundary = boundary;
    }

    /// <summary>Pins <c>grid</c> to a fixed value.</summary>
    public AnyReservoirSimulation WithGrid(GeologicalGrid value) {
        return new AnyReservoirSimulation(new FixedValue<GeologicalGrid>(value), _wells, _boundary);
    }

    /// <summary>Draws <c>grid</c> from <c>AnyValidGrid()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservoirSimulation WithGrid(Func<AnyGeologicalGrid, AnyGeologicalGrid> configure) {
        return new AnyReservoirSimulation(configure(AnyValidGrid()), _wells, _boundary);
    }

    /// <summary>Pins <c>wells</c> to a fixed value.</summary>
    public AnyReservoirSimulation WithWells(IReadOnlyCollection<Well> value) {
        return new AnyReservoirSimulation(_grid, new FixedValue<IReadOnlyCollection<Well>>(value), _boundary);
    }

    /// <summary>Draws <c>wells</c> from <c>AnyValidWells()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservoirSimulation WithWells(Func<AnyList<Well>, AnyList<Well>> configure) {
        return new AnyReservoirSimulation(_grid, configure(AnyValidWells()), _boundary);
    }

    /// <summary>Pins <c>boundary</c> to a fixed value.</summary>
    public AnyReservoirSimulation WithBoundary(BoundaryCondition value) {
        return new AnyReservoirSimulation(_grid, _wells, new FixedValue<BoundaryCondition>(value));
    }

    /// <summary>Draws <c>boundary</c> from <c>AnyValidBoundary()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservoirSimulation WithBoundary(Func<AnyBoundaryCondition, AnyBoundaryCondition> configure) {
        return new AnyReservoirSimulation(_grid, _wells, configure(AnyValidBoundary()));
    }

    /// <summary>Produces one arbitrary <see cref="ReservoirSimulation" />.</summary>
    public ReservoirSimulation Generate() {
        return new ReservoirSimulation(_grid.Generate(),
                                       _wells.Generate(),
                                       _boundary.Generate());
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
