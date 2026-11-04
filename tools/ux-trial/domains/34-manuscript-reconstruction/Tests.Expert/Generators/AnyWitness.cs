// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Witness --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Witness" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWitness : IAny<Witness> {

    private IAny<WitnessSiglum> _siglum;
    private IAny<string>        _repository;
    private IAny<int>           _folioCount;

    private AnyWitnessSiglum _siglumRecipe;
    private AnyString        _repositoryRecipe;
    private AnyInt32         _datedFromRecipe;
    private AnyInt32         _datedToRecipe;
    private AnyInt32         _folioCountRecipe;

    private int? _datedFromPin;
    private int? _datedToPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWitness()
        : this(siglum:     AnyValidSiglum(),
               repository: AnyValidRepository(),
               datedFrom:  AnyValidDatedFrom(),
               datedTo:    AnyValidDatedTo(),
               folioCount: AnyValidFolioCount()) { }

    private static AnyWitnessSiglum AnyValidSiglum() {
        return new AnyWitnessSiglum();
    }

    private static AnyString AnyValidRepository() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidDatedFrom() {
        return Any.Int32().GreaterThanOrEqualTo(Witness.EarliestYear);
    }

    private static AnyInt32 AnyValidDatedTo() {
        return Any.Int32().LessThanOrEqualTo(Witness.LatestYear);
    }

    private static AnyInt32 AnyValidFolioCount() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyWitness(AnyWitnessSiglum siglum,
                       AnyString        repository,
                       AnyInt32         datedFrom,
                       AnyInt32         datedTo,
                       AnyInt32         folioCount) {
        _siglum              = siglum;
        _siglumRecipe        = siglum;
        _repository          = repository;
        _repositoryRecipe    = repository;
        _datedFromRecipe     = datedFrom;
        _datedToRecipe       = datedTo;
        _folioCount          = folioCount;
        _folioCountRecipe    = folioCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyWitness(AnyWitness from) {
        _siglum              = from._siglum;
        _siglumRecipe        = from._siglumRecipe;
        _repository          = from._repository;
        _repositoryRecipe    = from._repositoryRecipe;
        _datedFromRecipe     = from._datedFromRecipe;
        _datedFromPin        = from._datedFromPin;
        _datedToRecipe       = from._datedToRecipe;
        _datedToPin          = from._datedToPin;
        _folioCount          = from._folioCount;
        _folioCountRecipe    = from._folioCountRecipe;
    }

    /// <summary>Draws <c>datedFrom</c> and <c>datedTo</c>, <c>datedFrom</c> never after <c>datedTo</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (int DatedFrom, int DatedTo) DatedFromAndDatedTo() {
        if (_datedFromPin is { } datedFrom) { return (datedFrom, _datedToPin ?? _datedToRecipe.GreaterThanOrEqualTo(datedFrom).Generate()); }
        if (_datedToPin is { } datedTo) { return (_datedFromRecipe.LessThanOrEqualTo(datedTo).Generate(), datedTo); }

        return Any.Pair(_datedFromRecipe, _datedToRecipe).Having.Item1LessThanOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>siglum</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWitness WithSiglum(WitnessSiglum value) {
        AnyWitness copy = new AnyWitness(this);

        copy._siglum = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>siglum</c> is drawn from — <c>AnyValidSiglum()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWitnessSiglum, AnyWitnessSiglum&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWitness WithSiglum(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWitness copy = new AnyWitness(this);

        copy._siglumRecipe = configure(_siglumRecipe);
        copy._siglum       = copy._siglumRecipe;

        return copy;
    }

    /// <summary>Pins <c>repository</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWitness WithRepository(string value) {
        AnyWitness copy = new AnyWitness(this);

        copy._repository = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>repository</c> is drawn from — <c>AnyValidRepository()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWitness WithRepository(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWitness copy = new AnyWitness(this);

        copy._repositoryRecipe = configure(_repositoryRecipe);
        copy._repository       = copy._repositoryRecipe;

        return copy;
    }

    /// <summary>Pins <c>datedFrom</c> to a fixed value, which bounds <c>datedTo</c>; its recipe is kept for a later refinement.</summary>
    public AnyWitness WithDatedFrom(int value) {
        AnyWitness copy = new AnyWitness(this);

        copy._datedFromPin        = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>datedFrom</c> is drawn from — <c>AnyValidDatedFrom()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>datedTo</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWitness WithDatedFrom(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWitness copy = new AnyWitness(this);

        copy._datedFromRecipe     = configure(_datedFromRecipe);
        copy._datedFromPin        = null;

        return copy;
    }

    /// <summary>Pins <c>datedTo</c> to a fixed value, which bounds <c>datedFrom</c>; its recipe is kept for a later refinement.</summary>
    public AnyWitness WithDatedTo(int value) {
        AnyWitness copy = new AnyWitness(this);

        copy._datedToPin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>datedTo</c> is drawn from — <c>AnyValidDatedTo()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>datedFrom</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWitness WithDatedTo(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWitness copy = new AnyWitness(this);

        copy._datedToRecipe       = configure(_datedToRecipe);
        copy._datedToPin          = null;

        return copy;
    }

    /// <summary>Pins <c>folioCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWitness WithFolioCount(int value) {
        AnyWitness copy = new AnyWitness(this);

        copy._folioCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>folioCount</c> is drawn from — <c>AnyValidFolioCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWitness WithFolioCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWitness copy = new AnyWitness(this);

        copy._folioCountRecipe = configure(_folioCountRecipe);
        copy._folioCount       = copy._folioCountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Witness" />.</summary>
    public Witness Generate() {
        (int datedFrom, int datedTo) = DatedFromAndDatedTo();

        return new Witness(_siglum.Generate(),
                           _repository.Generate(),
                           datedFrom,
                           datedTo,
                           _folioCount.Generate());
    }

}
