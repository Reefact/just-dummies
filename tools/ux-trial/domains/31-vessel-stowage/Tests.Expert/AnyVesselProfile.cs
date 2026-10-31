// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VesselProfile" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVesselProfile : IAny<VesselProfile> {

    private readonly IAny<string>  _name;
    private readonly IAny<int>     _bayCount;
    private readonly IAny<int>     _rowCount;
    private readonly IAny<int>     _holdTiers;
    private readonly IAny<int>     _deckTiers;
    private readonly IAny<decimal> _maxStackWeightTonnes;
    private readonly IAny<decimal> _lightshipTonnes;
    private readonly IAny<double>  _lightshipVcgMetres;
    private readonly IAny<decimal> _maxDisplacementTonnes;
    private readonly IAny<double>  _maxVcgMetres;
    private readonly IAny<double>  _maxTcgMetres;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVesselProfile()
        : this(name:                  AnyValidName(),
               bayCount:              AnyValidBayCount(),
               rowCount:              AnyValidRowCount(),
               holdTiers:             AnyValidHoldTiers(),
               deckTiers:             AnyValidDeckTiers(),
               maxStackWeightTonnes:  AnyValidMaxStackWeightTonnes(),
               lightshipTonnes:       AnyValidLightshipTonnes(),
               lightshipVcgMetres:    AnyValidLightshipVcgMetres(),
               maxDisplacementTonnes: AnyValidMaxDisplacementTonnes(),
               maxVcgMetres:          AnyValidMaxVcgMetres(),
               maxTcgMetres:          AnyValidMaxTcgMetres()) { }

    private static AnyString AnyValidName() {
        return Any.String().Alpha().WithLengthBetween(3, 30);
    }

    private static AnyInt32 AnyValidBayCount() {
        return Any.Int32().Between(1, 99);
    }

    private static AnyInt32 AnyValidRowCount() {
        return Any.Int32().Between(1, 21);
    }

    private static AnyInt32 AnyValidHoldTiers() {
        return Any.Int32().Between(1, 10);
    }

    private static AnyInt32 AnyValidDeckTiers() {
        return Any.Int32().Between(0, 9);
    }

    private static AnyDecimal AnyValidMaxStackWeightTonnes() {
        return Any.Decimal().Between(1m, 400m).WithScale(1);
    }

    // Lightship and maximum displacement are tied (max > lightship): the two bases draw from
    // disjoint ranges, so the rule holds by construction whatever each one draws.
    private static AnyDecimal AnyValidLightshipTonnes() {
        return Any.Decimal().Between(2_000m, 60_000m).WithScale(0);
    }

    // Same for the VCG pair (max > lightship VCG).
    private static AnyDouble AnyValidLightshipVcgMetres() {
        return Any.Double().Between(4.0, 12.0);
    }

    private static AnyDecimal AnyValidMaxDisplacementTonnes() {
        return Any.Decimal().Between(60_001m, 250_000m).WithScale(0);
    }

    private static AnyDouble AnyValidMaxVcgMetres() {
        return Any.Double().Between(12.5, 30.0);
    }

    private static AnyDouble AnyValidMaxTcgMetres() {
        return Any.Double().Between(0.5, 10.0);
    }

    private AnyVesselProfile(IAny<string>  name,
                             IAny<int>     bayCount,
                             IAny<int>     rowCount,
                             IAny<int>     holdTiers,
                             IAny<int>     deckTiers,
                             IAny<decimal> maxStackWeightTonnes,
                             IAny<decimal> lightshipTonnes,
                             IAny<double>  lightshipVcgMetres,
                             IAny<decimal> maxDisplacementTonnes,
                             IAny<double>  maxVcgMetres,
                             IAny<double>  maxTcgMetres) {
        _name                  = name;
        _bayCount              = bayCount;
        _rowCount              = rowCount;
        _holdTiers             = holdTiers;
        _deckTiers             = deckTiers;
        _maxStackWeightTonnes  = maxStackWeightTonnes;
        _lightshipTonnes       = lightshipTonnes;
        _lightshipVcgMetres    = lightshipVcgMetres;
        _maxDisplacementTonnes = maxDisplacementTonnes;
        _maxVcgMetres          = maxVcgMetres;
        _maxTcgMetres          = maxTcgMetres;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyVesselProfile WithName(string value) {
        return new AnyVesselProfile(new FixedValue<string>(value), _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithName(Func<AnyString, AnyString> configure) {
        return new AnyVesselProfile(configure(AnyValidName()), _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>bayCount</c> to a fixed value.</summary>
    public AnyVesselProfile WithBayCount(int value) {
        return new AnyVesselProfile(_name, new FixedValue<int>(value), _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>bayCount</c> from <c>AnyValidBayCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithBayCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVesselProfile(_name, configure(AnyValidBayCount()), _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>rowCount</c> to a fixed value.</summary>
    public AnyVesselProfile WithRowCount(int value) {
        return new AnyVesselProfile(_name, _bayCount, new FixedValue<int>(value), _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>rowCount</c> from <c>AnyValidRowCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithRowCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVesselProfile(_name, _bayCount, configure(AnyValidRowCount()), _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>holdTiers</c> to a fixed value.</summary>
    public AnyVesselProfile WithHoldTiers(int value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, new FixedValue<int>(value), _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>holdTiers</c> from <c>AnyValidHoldTiers()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithHoldTiers(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, configure(AnyValidHoldTiers()), _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>deckTiers</c> to a fixed value.</summary>
    public AnyVesselProfile WithDeckTiers(int value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, new FixedValue<int>(value), _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>deckTiers</c> from <c>AnyValidDeckTiers()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithDeckTiers(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, configure(AnyValidDeckTiers()), _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>maxStackWeightTonnes</c> to a fixed value.</summary>
    public AnyVesselProfile WithMaxStackWeightTonnes(decimal value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, new FixedValue<decimal>(value), _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>maxStackWeightTonnes</c> from <c>AnyValidMaxStackWeightTonnes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithMaxStackWeightTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, configure(AnyValidMaxStackWeightTonnes()), _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>lightshipTonnes</c> to a fixed value.</summary>
    public AnyVesselProfile WithLightshipTonnes(decimal value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, new FixedValue<decimal>(value), _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>lightshipTonnes</c> from <c>AnyValidLightshipTonnes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithLightshipTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, configure(AnyValidLightshipTonnes()), _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>lightshipVcgMetres</c> to a fixed value.</summary>
    public AnyVesselProfile WithLightshipVcgMetres(double value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, new FixedValue<double>(value), _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>lightshipVcgMetres</c> from <c>AnyValidLightshipVcgMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithLightshipVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, configure(AnyValidLightshipVcgMetres()), _maxDisplacementTonnes, _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>maxDisplacementTonnes</c> to a fixed value.</summary>
    public AnyVesselProfile WithMaxDisplacementTonnes(decimal value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, new FixedValue<decimal>(value), _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Draws <c>maxDisplacementTonnes</c> from <c>AnyValidMaxDisplacementTonnes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithMaxDisplacementTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, configure(AnyValidMaxDisplacementTonnes()), _maxVcgMetres, _maxTcgMetres);
    }

    /// <summary>Pins <c>maxVcgMetres</c> to a fixed value.</summary>
    public AnyVesselProfile WithMaxVcgMetres(double value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, new FixedValue<double>(value), _maxTcgMetres);
    }

    /// <summary>Draws <c>maxVcgMetres</c> from <c>AnyValidMaxVcgMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithMaxVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, configure(AnyValidMaxVcgMetres()), _maxTcgMetres);
    }

    /// <summary>Pins <c>maxTcgMetres</c> to a fixed value.</summary>
    public AnyVesselProfile WithMaxTcgMetres(double value) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>maxTcgMetres</c> from <c>AnyValidMaxTcgMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVesselProfile WithMaxTcgMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVesselProfile(_name, _bayCount, _rowCount, _holdTiers, _deckTiers, _maxStackWeightTonnes, _lightshipTonnes, _lightshipVcgMetres, _maxDisplacementTonnes, _maxVcgMetres, configure(AnyValidMaxTcgMetres()));
    }

    /// <summary>Produces one arbitrary <see cref="VesselProfile" />.</summary>
    public VesselProfile Generate() {
        return new VesselProfile(_name.Generate(),
                                 _bayCount.Generate(),
                                 _rowCount.Generate(),
                                 _holdTiers.Generate(),
                                 _deckTiers.Generate(),
                                 _maxStackWeightTonnes.Generate(),
                                 _lightshipTonnes.Generate(),
                                 _lightshipVcgMetres.Generate(),
                                 _maxDisplacementTonnes.Generate(),
                                 _maxVcgMetres.Generate(),
                                 _maxTcgMetres.Generate());
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
