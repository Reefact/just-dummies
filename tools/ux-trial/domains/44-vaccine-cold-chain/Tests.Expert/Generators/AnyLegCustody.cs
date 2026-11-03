// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LegCustody --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using ColdChain.Domain;
using Freight.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="LegCustody" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLegCustody : IAny<LegCustody> {

    private IAny<int>            _legIndex;
    private IAny<Leg>            _leg;
    private IAny<CustodianId>    _custodian;
    private IAny<DateTimeOffset> _acceptedAt;

    private AnyInt32          _legIndexRecipe;
    private AnyLeg            _legRecipe;
    private AnyCustodianId    _custodianRecipe;
    private AnyDateTimeOffset _acceptedAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLegCustody()
        : this(legIndex:   AnyValidLegIndex(),
               leg:        AnyValidLeg(),
               custodian:  AnyValidCustodian(),
               acceptedAt: AnyValidAcceptedAt()) { }

    private static AnyInt32 AnyValidLegIndex() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyLeg AnyValidLeg() {
        return new AnyLeg();
    }

    private static AnyCustodianId AnyValidCustodian() {
        return new AnyCustodianId();
    }

    private static AnyDateTimeOffset AnyValidAcceptedAt() {
        return Any.DateTimeOffset();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLegCustody(AnyInt32          legIndex,
                          AnyLeg            leg,
                          AnyCustodianId    custodian,
                          AnyDateTimeOffset acceptedAt) {
        _legIndex         = legIndex;
        _legIndexRecipe   = legIndex;
        _leg              = leg;
        _legRecipe        = leg;
        _custodian        = custodian;
        _custodianRecipe  = custodian;
        _acceptedAt       = acceptedAt;
        _acceptedAtRecipe = acceptedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLegCustody(AnyLegCustody from) {
        _legIndex         = from._legIndex;
        _legIndexRecipe   = from._legIndexRecipe;
        _leg              = from._leg;
        _legRecipe        = from._legRecipe;
        _custodian        = from._custodian;
        _custodianRecipe  = from._custodianRecipe;
        _acceptedAt       = from._acceptedAt;
        _acceptedAtRecipe = from._acceptedAtRecipe;
    }

    /// <summary>Pins <c>legIndex</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLegCustody WithLegIndex(int value) {
        AnyLegCustody copy = new AnyLegCustody(this);

        copy._legIndex = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>legIndex</c> is drawn from — <c>AnyValidLegIndex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLegCustody WithLegIndex(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLegCustody copy = new AnyLegCustody(this);

        copy._legIndexRecipe = configure(_legIndexRecipe);
        copy._legIndex       = copy._legIndexRecipe;

        return copy;
    }

    /// <summary>Pins <c>leg</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLegCustody WithLeg(Leg value) {
        AnyLegCustody copy = new AnyLegCustody(this);

        copy._leg = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>leg</c> is drawn from — <c>AnyValidLeg()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLeg, AnyLeg&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLegCustody WithLeg(Func<AnyLeg, AnyLeg> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLegCustody copy = new AnyLegCustody(this);

        copy._legRecipe = configure(_legRecipe);
        copy._leg       = copy._legRecipe;

        return copy;
    }

    /// <summary>Pins <c>custodian</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLegCustody WithCustodian(CustodianId value) {
        AnyLegCustody copy = new AnyLegCustody(this);

        copy._custodian = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>custodian</c> is drawn from — <c>AnyValidCustodian()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCustodianId, AnyCustodianId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLegCustody WithCustodian(Func<AnyCustodianId, AnyCustodianId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLegCustody copy = new AnyLegCustody(this);

        copy._custodianRecipe = configure(_custodianRecipe);
        copy._custodian       = copy._custodianRecipe;

        return copy;
    }

    /// <summary>Pins <c>acceptedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLegCustody WithAcceptedAt(DateTimeOffset value) {
        AnyLegCustody copy = new AnyLegCustody(this);

        copy._acceptedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>acceptedAt</c> is drawn from — <c>AnyValidAcceptedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLegCustody WithAcceptedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLegCustody copy = new AnyLegCustody(this);

        copy._acceptedAtRecipe = configure(_acceptedAtRecipe);
        copy._acceptedAt       = copy._acceptedAtRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LegCustody" />.</summary>
    public LegCustody Generate() {
        return new LegCustody(_legIndex.Generate(),
                              _leg.Generate(),
                              _custodian.Generate(),
                              _acceptedAt.Generate());
    }

}
