// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LoadFlowResult --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;
using System.Numerics;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LoadFlowResult" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLoadFlowResult : IAny<LoadFlowResult> {

    private IAny<int>                                 _iterations;
    private IAny<IReadOnlyDictionary<BusId, Complex>> _voltagesPu;
    private IAny<IReadOnlyList<BranchFlow>>           _branchFlows;
    private IAny<PowerInjection>                      _slackGeneration;

    private AnyInt32                      _iterationsRecipe;
    private AnyDictionary<BusId, Complex> _voltagesPuRecipe;
    private AnyList<BranchFlow>           _branchFlowsRecipe;
    private AnyPowerInjection             _slackGenerationRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLoadFlowResult()
        : this(iterations:      AnyValidIterations(),
               voltagesPu:      AnyValidVoltagesPu(),
               branchFlows:     AnyValidBranchFlows(),
               slackGeneration: AnyValidSlackGeneration()) { }

    private static AnyInt32 AnyValidIterations() {
        return Any.Int32().Positive();
    }

    private static AnyDictionary<BusId, Complex> AnyValidVoltagesPu() {
        return Any.DictionaryOf(new AnyBusId(), new AnyComplex()).WithMinCount(2);
    }

    private static AnyList<BranchFlow> AnyValidBranchFlows() {
        return Any.ListOf(new AnyBranchFlow());
    }

    private static AnyPowerInjection AnyValidSlackGeneration() {
        return new AnyPowerInjection();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLoadFlowResult(AnyInt32                      iterations,
                              AnyDictionary<BusId, Complex> voltagesPu,
                              AnyList<BranchFlow>           branchFlows,
                              AnyPowerInjection             slackGeneration) {
        _iterations            = iterations;
        _iterationsRecipe      = iterations;
        _voltagesPu            = voltagesPu;
        _voltagesPuRecipe      = voltagesPu;
        _branchFlows           = branchFlows;
        _branchFlowsRecipe     = branchFlows;
        _slackGeneration       = slackGeneration;
        _slackGenerationRecipe = slackGeneration;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLoadFlowResult(AnyLoadFlowResult from) {
        _iterations            = from._iterations;
        _iterationsRecipe      = from._iterationsRecipe;
        _voltagesPu            = from._voltagesPu;
        _voltagesPuRecipe      = from._voltagesPuRecipe;
        _branchFlows           = from._branchFlows;
        _branchFlowsRecipe     = from._branchFlowsRecipe;
        _slackGeneration       = from._slackGeneration;
        _slackGenerationRecipe = from._slackGenerationRecipe;
    }

    /// <summary>Pins <c>iterations</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadFlowResult WithIterations(int value) {
        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._iterations = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>iterations</c> is drawn from — <c>AnyValidIterations()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadFlowResult WithIterations(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._iterationsRecipe = configure(_iterationsRecipe);
        copy._iterations       = copy._iterationsRecipe;

        return copy;
    }

    /// <summary>Pins <c>voltagesPu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadFlowResult WithVoltagesPu(IReadOnlyDictionary<BusId, Complex> value) {
        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._voltagesPu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>voltagesPu</c> is drawn from — <c>AnyValidVoltagesPu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDictionary&lt;BusId, Complex&gt;, AnyDictionary&lt;BusId, Complex&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadFlowResult WithVoltagesPu(Func<AnyDictionary<BusId, Complex>, AnyDictionary<BusId, Complex>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._voltagesPuRecipe = configure(_voltagesPuRecipe);
        copy._voltagesPu       = copy._voltagesPuRecipe;

        return copy;
    }

    /// <summary>Pins <c>branchFlows</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadFlowResult WithBranchFlows(IReadOnlyList<BranchFlow> value) {
        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._branchFlows = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>branchFlows</c> is drawn from — <c>AnyValidBranchFlows()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;BranchFlow&gt;, AnyList&lt;BranchFlow&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadFlowResult WithBranchFlows(Func<AnyList<BranchFlow>, AnyList<BranchFlow>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._branchFlowsRecipe = configure(_branchFlowsRecipe);
        copy._branchFlows       = copy._branchFlowsRecipe;

        return copy;
    }

    /// <summary>Pins <c>slackGeneration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoadFlowResult WithSlackGeneration(PowerInjection value) {
        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._slackGeneration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>slackGeneration</c> is drawn from — <c>AnyValidSlackGeneration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPowerInjection, AnyPowerInjection&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoadFlowResult WithSlackGeneration(Func<AnyPowerInjection, AnyPowerInjection> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoadFlowResult copy = new AnyLoadFlowResult(this);

        copy._slackGenerationRecipe = configure(_slackGenerationRecipe);
        copy._slackGeneration       = copy._slackGenerationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LoadFlowResult" />.</summary>
    public LoadFlowResult Generate() {
        return new LoadFlowResult(_iterations.Generate(),
                                  _voltagesPu.Generate(),
                                  _branchFlows.Generate(),
                                  _slackGeneration.Generate());
    }

}
