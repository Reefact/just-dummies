// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeologicalGrid --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeologicalGrid" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeologicalGrid : IAny<GeologicalGrid> {

    private IAny<int>                           _cellsX;
    private IAny<int>                           _cellsY;
    private IAny<int>                           _cellsZ;
    private IAny<double>                        _cellSizeMetres;
    private AnyRockCell _cells;

    private AnyInt32          _cellsXRecipe;
    private AnyInt32          _cellsYRecipe;
    private AnyInt32          _cellsZRecipe;
    private AnyDouble         _cellSizeMetresRecipe;
    private AnyRockCell _cellsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeologicalGrid()
        : this(cellsX:         AnyValidCellsX(),
               cellsY:         AnyValidCellsY(),
               cellsZ:         AnyValidCellsZ(),
               cellSizeMetres: AnyValidCellSizeMetres(),
               cells:          AnyValidCells()) { }

    private static AnyInt32 AnyValidCellsX() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsY() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsZ() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyDouble AnyValidCellSizeMetres() {
        return Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres);
    }

    // The cells are not drawn as a list: the grid owns the rule "exactly X*Y*Z cells, one per coordinate",
    // so Generate() builds them from the dimensions, from this template with only the coordinate replaced.
    private static AnyRockCell AnyValidCells() {
        return new AnyRockCell();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGeologicalGrid(AnyInt32          cellsX,
                              AnyInt32          cellsY,
                              AnyInt32          cellsZ,
                              AnyDouble         cellSizeMetres,
                              AnyRockCell cells) {
        _cellsX               = cellsX;
        _cellsXRecipe         = cellsX;
        _cellsY               = cellsY;
        _cellsYRecipe         = cellsY;
        _cellsZ               = cellsZ;
        _cellsZRecipe         = cellsZ;
        _cellSizeMetres       = cellSizeMetres;
        _cellSizeMetresRecipe = cellSizeMetres;
        _cells                = cells;
        _cellsRecipe          = cells;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGeologicalGrid(AnyGeologicalGrid from) {
        _cellsX               = from._cellsX;
        _cellsXRecipe         = from._cellsXRecipe;
        _cellsY               = from._cellsY;
        _cellsYRecipe         = from._cellsYRecipe;
        _cellsZ               = from._cellsZ;
        _cellsZRecipe         = from._cellsZRecipe;
        _cellSizeMetres       = from._cellSizeMetres;
        _cellSizeMetresRecipe = from._cellSizeMetresRecipe;
        _cells                = from._cells;
        _cellsRecipe          = from._cellsRecipe;
    }

    /// <summary>Pins <c>cellsX</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeologicalGrid WithCellsX(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsX = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cellsX</c> is drawn from — <c>AnyValidCellsX()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeologicalGrid WithCellsX(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsXRecipe = configure(_cellsXRecipe);
        copy._cellsX       = copy._cellsXRecipe;

        return copy;
    }

    /// <summary>Pins <c>cellsY</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeologicalGrid WithCellsY(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsY = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cellsY</c> is drawn from — <c>AnyValidCellsY()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeologicalGrid WithCellsY(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsYRecipe = configure(_cellsYRecipe);
        copy._cellsY       = copy._cellsYRecipe;

        return copy;
    }

    /// <summary>Pins <c>cellsZ</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeologicalGrid WithCellsZ(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsZ = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cellsZ</c> is drawn from — <c>AnyValidCellsZ()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeologicalGrid WithCellsZ(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsZRecipe = configure(_cellsZRecipe);
        copy._cellsZ       = copy._cellsZRecipe;

        return copy;
    }

    /// <summary>Pins <c>cellSizeMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeologicalGrid WithCellSizeMetres(double value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellSizeMetres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cellSizeMetres</c> is drawn from — <c>AnyValidCellSizeMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeologicalGrid WithCellSizeMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellSizeMetresRecipe = configure(_cellSizeMetresRecipe);
        copy._cellSizeMetres       = copy._cellSizeMetresRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>cells</c> is drawn from — <c>AnyValidCells()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;RockCell&gt;, AnyList&lt;RockCell&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeologicalGrid WithCells(Func<AnyRockCell, AnyRockCell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsRecipe = configure(_cellsRecipe);
        copy._cells       = copy._cellsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GeologicalGrid" />.</summary>
    public GeologicalGrid Generate() {
        int x = _cellsX.Generate();
        int y = _cellsY.Generate();
        int z = _cellsZ.Generate();

        List<RockCell> cells = new List<RockCell>();
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    cells.Add(_cellsRecipe.WithCoordinate(new CellCoordinate(i, j, k)).Generate());
                }
            }
        }

        return new GeologicalGrid(x, y, z, _cellSizeMetres.Generate(), cells);
    }

}
