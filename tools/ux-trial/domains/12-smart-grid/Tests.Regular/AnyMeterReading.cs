// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeterReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeterReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeterReading : IAny<MeterReading> {

    private IAny<DeliveryPointId> _deliveryPoint;
    private IAny<MeterIndex>      _index;
    private IAny<DateOnly>        _readingDate;
    private IAny<ReadingSource>   _source;

    private AnyDeliveryPointId     _deliveryPointRecipe;
    private AnyMeterIndex          _indexRecipe;
    private AnyDateOnly            _readingDateRecipe;
    private AnyEnum<ReadingSource> _sourceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeterReading()
        : this(deliveryPoint: AnyValidDeliveryPoint(),
               index:         AnyValidIndex(),
               readingDate:   AnyValidReadingDate(),
               source:        AnyValidSource()) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyMeterIndex AnyValidIndex() {
        return new AnyMeterIndex();
    }

    private static AnyDateOnly AnyValidReadingDate() {
        return Any.DateOnly().Between(MeterReading.SmartMeteringEpoch, new DateOnly(2100, 1, 1));
    }

    private static AnyEnum<ReadingSource> AnyValidSource() {
        return Any.Enum<ReadingSource>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeterReading(AnyDeliveryPointId     deliveryPoint,
                            AnyMeterIndex          index,
                            AnyDateOnly            readingDate,
                            AnyEnum<ReadingSource> source) {
        _deliveryPoint       = deliveryPoint;
        _deliveryPointRecipe = deliveryPoint;
        _index               = index;
        _indexRecipe         = index;
        _readingDate         = readingDate;
        _readingDateRecipe   = readingDate;
        _source              = source;
        _sourceRecipe        = source;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeterReading(AnyMeterReading from) {
        _deliveryPoint       = from._deliveryPoint;
        _deliveryPointRecipe = from._deliveryPointRecipe;
        _index               = from._index;
        _indexRecipe         = from._indexRecipe;
        _readingDate         = from._readingDate;
        _readingDateRecipe   = from._readingDateRecipe;
        _source              = from._source;
        _sourceRecipe        = from._sourceRecipe;
    }

    /// <summary>Pins <c>deliveryPoint</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeterReading WithDeliveryPoint(DeliveryPointId value) {
        AnyMeterReading copy = new AnyMeterReading(this);

        copy._deliveryPoint = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deliveryPoint</c> is drawn from — <c>AnyValidDeliveryPoint()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDeliveryPointId, AnyDeliveryPointId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeterReading WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeterReading copy = new AnyMeterReading(this);

        copy._deliveryPointRecipe = configure(_deliveryPointRecipe);
        copy._deliveryPoint       = copy._deliveryPointRecipe;

        return copy;
    }

    /// <summary>Pins <c>index</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeterReading WithIndex(MeterIndex value) {
        AnyMeterReading copy = new AnyMeterReading(this);

        copy._index = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>index</c> is drawn from — <c>AnyValidIndex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeterIndex, AnyMeterIndex&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeterReading WithIndex(Func<AnyMeterIndex, AnyMeterIndex> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeterReading copy = new AnyMeterReading(this);

        copy._indexRecipe = configure(_indexRecipe);
        copy._index       = copy._indexRecipe;

        return copy;
    }

    /// <summary>Pins <c>readingDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeterReading WithReadingDate(DateOnly value) {
        AnyMeterReading copy = new AnyMeterReading(this);

        copy._readingDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>readingDate</c> is drawn from — <c>AnyValidReadingDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeterReading WithReadingDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeterReading copy = new AnyMeterReading(this);

        copy._readingDateRecipe = configure(_readingDateRecipe);
        copy._readingDate       = copy._readingDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>source</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeterReading WithSource(ReadingSource value) {
        AnyMeterReading copy = new AnyMeterReading(this);

        copy._source = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>source</c> is drawn from — <c>AnyValidSource()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ReadingSource&gt;, AnyEnum&lt;ReadingSource&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeterReading WithSource(Func<AnyEnum<ReadingSource>, AnyEnum<ReadingSource>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeterReading copy = new AnyMeterReading(this);

        copy._sourceRecipe = configure(_sourceRecipe);
        copy._source       = copy._sourceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MeterReading" />.</summary>
    public MeterReading Generate() {
        return new MeterReading(_deliveryPoint.Generate(),
                                _index.Generate(),
                                _readingDate.Generate(),
                                _source.Generate());
    }

}
