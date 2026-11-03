// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TemperatureHistory --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using ColdChain.Domain;
using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TemperatureHistory" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperatureHistory : IAny<TemperatureHistory> {

    private IAny<DataLogger>       _logger;
    private IAny<TemperatureRange> _labelledRange;

    private AnyDataLogger       _loggerRecipe;
    private AnyTemperatureRange _labelledRangeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperatureHistory()
        : this(logger:        AnyValidLogger(),
               labelledRange: AnyValidLabelledRange()) { }

    private static AnyDataLogger AnyValidLogger() {
        return new AnyDataLogger();
    }

    private static AnyTemperatureRange AnyValidLabelledRange() {
        return new AnyTemperatureRange();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTemperatureHistory(AnyDataLogger       logger,
                                  AnyTemperatureRange labelledRange) {
        _logger              = logger;
        _loggerRecipe        = logger;
        _labelledRange       = labelledRange;
        _labelledRangeRecipe = labelledRange;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTemperatureHistory(AnyTemperatureHistory from) {
        _logger              = from._logger;
        _loggerRecipe        = from._loggerRecipe;
        _labelledRange       = from._labelledRange;
        _labelledRangeRecipe = from._labelledRangeRecipe;
    }

    /// <summary>Pins <c>logger</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTemperatureHistory WithLogger(DataLogger value) {
        AnyTemperatureHistory copy = new AnyTemperatureHistory(this);

        copy._logger = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>logger</c> is drawn from — <c>AnyValidLogger()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDataLogger, AnyDataLogger&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperatureHistory WithLogger(Func<AnyDataLogger, AnyDataLogger> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperatureHistory copy = new AnyTemperatureHistory(this);

        copy._loggerRecipe = configure(_loggerRecipe);
        copy._logger       = copy._loggerRecipe;

        return copy;
    }

    /// <summary>Pins <c>labelledRange</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTemperatureHistory WithLabelledRange(TemperatureRange value) {
        AnyTemperatureHistory copy = new AnyTemperatureHistory(this);

        copy._labelledRange = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>labelledRange</c> is drawn from — <c>AnyValidLabelledRange()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureRange, AnyTemperatureRange&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperatureHistory WithLabelledRange(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperatureHistory copy = new AnyTemperatureHistory(this);

        copy._labelledRangeRecipe = configure(_labelledRangeRecipe);
        copy._labelledRange       = copy._labelledRangeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TemperatureHistory" />.</summary>
    public TemperatureHistory Generate() {
        return new TemperatureHistory(_logger.Generate(),
                                      _labelledRange.Generate());
    }

}
