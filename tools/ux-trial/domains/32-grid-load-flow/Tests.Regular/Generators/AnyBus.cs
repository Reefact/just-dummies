// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using GridLoadFlow.Domain;
using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="Bus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBus : IAny<Bus> {

    private IAny<BusId>          _id;
    private IAny<string>         _name;
    private IAny<BusType>        _type;
    private IAny<double>         _baseKv;
    private IAny<PowerInjection> _generation;
    private IAny<PowerInjection> _load;
    private IAny<double?>        _voltageSetpointPu;
    private IAny<VoltageLimits>  _limits;

    private AnyBusId          _idRecipe;
    private AnyString         _nameRecipe;
    private AnyEnum<BusType>  _typeRecipe;
    private AnyDouble         _baseKvRecipe;
    private AnyPowerInjection _generationRecipe;
    private AnyPowerInjection _loadRecipe;
    private AnyDouble         _voltageSetpointPuRecipe;
    private AnyVoltageLimits  _limitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBus()
        : this(id:                AnyValidId(),
               name:              AnyValidName(),
               type:              AnyValidType(),
               baseKv:            AnyValidBaseKv(),
               generation:        AnyValidGeneration(),
               load:              AnyValidLoad(),
               voltageSetpointPu: AnyValidVoltageSetpointPu(),
               limits:            AnyValidLimits()) { }

    private static AnyBusId AnyValidId() {
        return new AnyBusId();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyEnum<BusType> AnyValidType() {
        return Any.Enum<BusType>();
    }

    private static AnyDouble AnyValidBaseKv() {
        return Any.Double().Positive().LessThanOrEqualTo(Bus.MaximumBaseKv);
    }

    private static AnyPowerInjection AnyValidGeneration() {
        return new AnyPowerInjection().WithActiveMw(p => p.GreaterThanOrEqualTo(0d));
    }

    private static AnyPowerInjection AnyValidLoad() {
        return new AnyPowerInjection();
    }

    private static AnyDouble AnyValidVoltageSetpointPu() {
        // Inside every band AnyValidLimits() can draw.
        return Any.Double().Between(0.95d, 1.05d);
    }

    private static AnyVoltageLimits AnyValidLimits() {
        // A band that holds every setpoint AnyValidVoltageSetpointPu() can draw.
        return new AnyVoltageLimits().WithMinimumPu(m => m.LessThanOrEqualTo(0.95d)).WithMaximumPu(m => m.GreaterThanOrEqualTo(1.05d));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBus(AnyBusId          id,
                   AnyString         name,
                   AnyEnum<BusType>  type,
                   AnyDouble         baseKv,
                   AnyPowerInjection generation,
                   AnyPowerInjection load,
                   AnyDouble         voltageSetpointPu,
                   AnyVoltageLimits  limits) {
        _id                      = id;
        _idRecipe                = id;
        _name                    = name;
        _nameRecipe              = name;
        _type                    = type;
        _typeRecipe              = type;
        _baseKv                  = baseKv;
        _baseKvRecipe            = baseKv;
        _generation              = generation;
        _generationRecipe        = generation;
        _load                    = load;
        _loadRecipe              = load;
        _voltageSetpointPu       = voltageSetpointPu.AsNullable();
        _voltageSetpointPuRecipe = voltageSetpointPu;
        _limits                  = limits;
        _limitsRecipe            = limits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBus(AnyBus from) {
        _id                      = from._id;
        _idRecipe                = from._idRecipe;
        _name                    = from._name;
        _nameRecipe              = from._nameRecipe;
        _type                    = from._type;
        _typeRecipe              = from._typeRecipe;
        _baseKv                  = from._baseKv;
        _baseKvRecipe            = from._baseKvRecipe;
        _generation              = from._generation;
        _generationRecipe        = from._generationRecipe;
        _load                    = from._load;
        _loadRecipe              = from._loadRecipe;
        _voltageSetpointPu       = from._voltageSetpointPu;
        _voltageSetpointPuRecipe = from._voltageSetpointPuRecipe;
        _limits                  = from._limits;
        _limitsRecipe            = from._limitsRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithId(BusId value) {
        AnyBus copy = new AnyBus(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithId(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithName(string value) {
        AnyBus copy = new AnyBus(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>type</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithType(BusType value) {
        AnyBus copy = new AnyBus(this);

        copy._type = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>type</c> is drawn from — <c>AnyValidType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;BusType&gt;, AnyEnum&lt;BusType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithType(Func<AnyEnum<BusType>, AnyEnum<BusType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._typeRecipe = configure(_typeRecipe);
        copy._type       = copy._typeRecipe;

        return copy;
    }

    /// <summary>Pins <c>baseKv</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithBaseKv(double value) {
        AnyBus copy = new AnyBus(this);

        copy._baseKv = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>baseKv</c> is drawn from — <c>AnyValidBaseKv()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithBaseKv(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._baseKvRecipe = configure(_baseKvRecipe);
        copy._baseKv       = copy._baseKvRecipe;

        return copy;
    }

    /// <summary>Pins <c>generation</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithGeneration(PowerInjection value) {
        AnyBus copy = new AnyBus(this);

        copy._generation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>generation</c> is drawn from — <c>AnyValidGeneration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPowerInjection, AnyPowerInjection&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithGeneration(Func<AnyPowerInjection, AnyPowerInjection> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._generationRecipe = configure(_generationRecipe);
        copy._generation       = copy._generationRecipe;

        return copy;
    }

    /// <summary>Pins <c>load</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithLoad(PowerInjection value) {
        AnyBus copy = new AnyBus(this);

        copy._load = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>load</c> is drawn from — <c>AnyValidLoad()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPowerInjection, AnyPowerInjection&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithLoad(Func<AnyPowerInjection, AnyPowerInjection> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._loadRecipe = configure(_loadRecipe);
        copy._load       = copy._loadRecipe;

        return copy;
    }

    /// <summary>Pins <c>voltageSetpointPu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithVoltageSetpointPu(double? value) {
        AnyBus copy = new AnyBus(this);

        copy._voltageSetpointPu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>voltageSetpointPu</c> is drawn from — <c>AnyValidVoltageSetpointPu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithVoltageSetpointPu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._voltageSetpointPuRecipe = configure(_voltageSetpointPuRecipe);
        copy._voltageSetpointPu       = copy._voltageSetpointPuRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>voltageSetpointPu</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyBus WithoutVoltageSetpointPu() {
        AnyBus copy = new AnyBus(this);

        copy._voltageSetpointPu = Any.Fixed<double?>(null);

        return copy;
    }

    /// <summary>Pins <c>limits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBus WithLimits(VoltageLimits value) {
        AnyBus copy = new AnyBus(this);

        copy._limits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>limits</c> is drawn from — <c>AnyValidLimits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVoltageLimits, AnyVoltageLimits&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBus WithLimits(Func<AnyVoltageLimits, AnyVoltageLimits> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBus copy = new AnyBus(this);

        copy._limitsRecipe = configure(_limitsRecipe);
        copy._limits       = copy._limitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Bus" />.</summary>
    public Bus Generate() {
        // A PQ bus takes no setpoint and no active generation; the other types need a setpoint.
        BusType         type       = _type.Generate();
        PowerInjection  generation = _generation.Generate();
        double?         setpoint   = _voltageSetpointPu.Generate();

        if (type == BusType.Pq) {
            setpoint   = null;
            generation = new PowerInjection(0d, generation.ReactiveMvar);
        }

        return new Bus(_id.Generate(),
                       _name.Generate(),
                       type,
                       _baseKv.Generate(),
                       generation,
                       _load.Generate(),
                       setpoint,
                       _limits.Generate());
    }

}
