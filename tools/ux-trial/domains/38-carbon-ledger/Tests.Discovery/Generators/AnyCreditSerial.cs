// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CreditSerial --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using CarbonLedger.Domain;
using JustDummies;

namespace CarbonLedger.Tests.Discovery.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CreditSerial" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCreditSerial : IAny<CreditSerial> {

    private IAny<string> _registry;
    private IAny<int>    _projectId;
    private IAny<int>    _vintage;
    private IAny<long>   _unitNumber;

    private AnyPattern _registryRecipe;
    private AnyInt32  _projectIdRecipe;
    private AnyInt32  _vintageRecipe;
    private AnyInt64  _unitNumberRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCreditSerial()
        : this(registry:   AnyValidRegistry(),
               projectId:  AnyValidProjectId(),
               vintage:    AnyValidVintage(),
               unitNumber: AnyValidUnitNumber()) { }

    private static AnyPattern AnyValidRegistry() {
        return Any.StringMatching("[A-Z]{2,5}");
    }

    private static AnyInt32 AnyValidProjectId() {
        return Any.Int32().Positive().LessThanOrEqualTo(CreditSerial.MaxProjectId);
    }

    private static AnyInt32 AnyValidVintage() {
        return Any.Int32().Between(CreditSerial.MinVintage, CreditSerial.MaxVintage);
    }

    private static AnyInt64 AnyValidUnitNumber() {
        return Any.Int64().Positive().LessThanOrEqualTo(CreditSerial.MaxUnitNumber);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCreditSerial(AnyPattern registry,
                            AnyInt32  projectId,
                            AnyInt32  vintage,
                            AnyInt64  unitNumber) {
        _registry         = registry;
        _registryRecipe   = registry;
        _projectId        = projectId;
        _projectIdRecipe  = projectId;
        _vintage          = vintage;
        _vintageRecipe    = vintage;
        _unitNumber       = unitNumber;
        _unitNumberRecipe = unitNumber;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCreditSerial(AnyCreditSerial from) {
        _registry         = from._registry;
        _registryRecipe   = from._registryRecipe;
        _projectId        = from._projectId;
        _projectIdRecipe  = from._projectIdRecipe;
        _vintage          = from._vintage;
        _vintageRecipe    = from._vintageRecipe;
        _unitNumber       = from._unitNumber;
        _unitNumberRecipe = from._unitNumberRecipe;
    }

    /// <summary>Pins <c>registry</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditSerial WithRegistry(string value) {
        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._registry = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>registry</c> is drawn from — <c>AnyValidRegistry()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditSerial WithRegistry(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._registryRecipe = configure(_registryRecipe);
        copy._registry       = copy._registryRecipe;

        return copy;
    }

    /// <summary>Pins <c>projectId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditSerial WithProjectId(int value) {
        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._projectId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>projectId</c> is drawn from — <c>AnyValidProjectId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditSerial WithProjectId(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._projectIdRecipe = configure(_projectIdRecipe);
        copy._projectId       = copy._projectIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>vintage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditSerial WithVintage(int value) {
        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._vintage = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>vintage</c> is drawn from — <c>AnyValidVintage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditSerial WithVintage(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._vintageRecipe = configure(_vintageRecipe);
        copy._vintage       = copy._vintageRecipe;

        return copy;
    }

    /// <summary>Pins <c>unitNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditSerial WithUnitNumber(long value) {
        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._unitNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitNumber</c> is drawn from — <c>AnyValidUnitNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt64, AnyInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditSerial WithUnitNumber(Func<AnyInt64, AnyInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditSerial copy = new AnyCreditSerial(this);

        copy._unitNumberRecipe = configure(_unitNumberRecipe);
        copy._unitNumber       = copy._unitNumberRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CreditSerial" />.</summary>
    public CreditSerial Generate() {
        return new CreditSerial(_registry.Generate(),
                                _projectId.Generate(),
                                _vintage.Generate(),
                                _unitNumber.Generate());
    }

}
