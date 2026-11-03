// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SerialisedPack --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Serialisation.Domain;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="SerialisedPack" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySerialisedPack : IAny<SerialisedPack> {

    private IAny<Gtin>         _gtin;
    private IAny<BatchNumber>  _batch;
    private IAny<DateOnly>     _expiresOn;
    private IAny<SerialNumber> _serial;

    private AnyGtin         _gtinRecipe;
    private AnyBatchNumber  _batchRecipe;
    private AnyDateOnly     _expiresOnRecipe;
    private AnySerialNumber _serialRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySerialisedPack()
        : this(gtin:      AnyValidGtin(),
               batch:     AnyValidBatch(),
               expiresOn: AnyValidExpiresOn(),
               serial:    AnyValidSerial()) { }

    private static AnyGtin AnyValidGtin() {
        return new AnyGtin();
    }

    private static AnyBatchNumber AnyValidBatch() {
        return new AnyBatchNumber();
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        return Any.DateOnly();
    }

    private static AnySerialNumber AnyValidSerial() {
        return new AnySerialNumber();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySerialisedPack(AnyGtin         gtin,
                              AnyBatchNumber  batch,
                              AnyDateOnly     expiresOn,
                              AnySerialNumber serial) {
        _gtin            = gtin;
        _gtinRecipe      = gtin;
        _batch           = batch;
        _batchRecipe     = batch;
        _expiresOn       = expiresOn;
        _expiresOnRecipe = expiresOn;
        _serial          = serial;
        _serialRecipe    = serial;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySerialisedPack(AnySerialisedPack from) {
        _gtin            = from._gtin;
        _gtinRecipe      = from._gtinRecipe;
        _batch           = from._batch;
        _batchRecipe     = from._batchRecipe;
        _expiresOn       = from._expiresOn;
        _expiresOnRecipe = from._expiresOnRecipe;
        _serial          = from._serial;
        _serialRecipe    = from._serialRecipe;
    }

    /// <summary>Pins <c>gtin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySerialisedPack WithGtin(Gtin value) {
        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._gtin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gtin</c> is drawn from — <c>AnyValidGtin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGtin, AnyGtin&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySerialisedPack WithGtin(Func<AnyGtin, AnyGtin> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._gtinRecipe = configure(_gtinRecipe);
        copy._gtin       = copy._gtinRecipe;

        return copy;
    }

    /// <summary>Pins <c>batch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySerialisedPack WithBatch(BatchNumber value) {
        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._batch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>batch</c> is drawn from — <c>AnyValidBatch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatchNumber, AnyBatchNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySerialisedPack WithBatch(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._batchRecipe = configure(_batchRecipe);
        copy._batch       = copy._batchRecipe;

        return copy;
    }

    /// <summary>Pins <c>expiresOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySerialisedPack WithExpiresOn(DateOnly value) {
        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._expiresOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>expiresOn</c> is drawn from — <c>AnyValidExpiresOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySerialisedPack WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._expiresOnRecipe = configure(_expiresOnRecipe);
        copy._expiresOn       = copy._expiresOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>serial</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySerialisedPack WithSerial(SerialNumber value) {
        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._serial = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>serial</c> is drawn from — <c>AnyValidSerial()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySerialNumber, AnySerialNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySerialisedPack WithSerial(Func<AnySerialNumber, AnySerialNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySerialisedPack copy = new AnySerialisedPack(this);

        copy._serialRecipe = configure(_serialRecipe);
        copy._serial       = copy._serialRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SerialisedPack" />.</summary>
    public SerialisedPack Generate() {
        return new SerialisedPack(_gtin.Generate(),
                                  _batch.Generate(),
                                  _expiresOn.Generate(),
                                  _serial.Generate());
    }

}
