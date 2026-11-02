// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Property --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Property" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyProperty : IAny<Property> {

    private IAny<CadastralReference>          _parcel;
    private IAny<PropertyKind>                _kind;
    private IAny<Surface>                     _livingArea;
    private IAny<EnergyPerformanceDiagnosis>  _diagnosis;
    private IAny<IReadOnlyList<DocumentLink>> _documents;

    private AnyCadastralReference         _parcelRecipe;
    private AnyEnum<PropertyKind>         _kindRecipe;
    private AnySurface                    _livingAreaRecipe;
    private AnyEnergyPerformanceDiagnosis _diagnosisRecipe;
    private AnyList<DocumentLink>         _documentsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyProperty()
        : this(parcel:     AnyValidParcel(),
               kind:       AnyValidKind(),
               livingArea: AnyValidLivingArea(),
               diagnosis:  AnyValidDiagnosis(),
               documents:  AnyValidDocuments()) { }

    private static AnyCadastralReference AnyValidParcel() {
        return new AnyCadastralReference();
    }

    private static AnyEnum<PropertyKind> AnyValidKind() {
        return Any.Enum<PropertyKind>();
    }

    private static AnySurface AnyValidLivingArea() {
        return new AnySurface();
    }

    private static AnyEnergyPerformanceDiagnosis AnyValidDiagnosis() {
        return new AnyEnergyPerformanceDiagnosis();
    }

    private static AnyList<DocumentLink> AnyValidDocuments() {
        return Any.ListOf(new AnyDocumentLink()).WithMaxCount(Property.MaxDocuments).DistinctBy(document => document.Location);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyProperty(AnyCadastralReference         parcel,
                        AnyEnum<PropertyKind>         kind,
                        AnySurface                    livingArea,
                        AnyEnergyPerformanceDiagnosis diagnosis,
                        AnyList<DocumentLink>         documents) {
        _parcel           = parcel;
        _parcelRecipe     = parcel;
        _kind             = kind;
        _kindRecipe       = kind;
        _livingArea       = livingArea;
        _livingAreaRecipe = livingArea;
        _diagnosis        = diagnosis;
        _diagnosisRecipe  = diagnosis;
        _documents        = documents;
        _documentsRecipe  = documents;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyProperty(AnyProperty from) {
        _parcel           = from._parcel;
        _parcelRecipe     = from._parcelRecipe;
        _kind             = from._kind;
        _kindRecipe       = from._kindRecipe;
        _livingArea       = from._livingArea;
        _livingAreaRecipe = from._livingAreaRecipe;
        _diagnosis        = from._diagnosis;
        _diagnosisRecipe  = from._diagnosisRecipe;
        _documents        = from._documents;
        _documentsRecipe  = from._documentsRecipe;
    }

    /// <summary>Pins <c>parcel</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProperty WithParcel(CadastralReference value) {
        AnyProperty copy = new AnyProperty(this);

        copy._parcel = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>parcel</c> is drawn from — <c>AnyValidParcel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCadastralReference, AnyCadastralReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProperty WithParcel(Func<AnyCadastralReference, AnyCadastralReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProperty copy = new AnyProperty(this);

        copy._parcelRecipe = configure(_parcelRecipe);
        copy._parcel       = copy._parcelRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProperty WithKind(PropertyKind value) {
        AnyProperty copy = new AnyProperty(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;PropertyKind&gt;, AnyEnum&lt;PropertyKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProperty WithKind(Func<AnyEnum<PropertyKind>, AnyEnum<PropertyKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProperty copy = new AnyProperty(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>livingArea</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProperty WithLivingArea(Surface value) {
        AnyProperty copy = new AnyProperty(this);

        copy._livingArea = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>livingArea</c> is drawn from — <c>AnyValidLivingArea()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySurface, AnySurface&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProperty WithLivingArea(Func<AnySurface, AnySurface> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProperty copy = new AnyProperty(this);

        copy._livingAreaRecipe = configure(_livingAreaRecipe);
        copy._livingArea       = copy._livingAreaRecipe;

        return copy;
    }

    /// <summary>Pins <c>diagnosis</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProperty WithDiagnosis(EnergyPerformanceDiagnosis value) {
        AnyProperty copy = new AnyProperty(this);

        copy._diagnosis = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>diagnosis</c> is drawn from — <c>AnyValidDiagnosis()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnergyPerformanceDiagnosis, AnyEnergyPerformanceDiagnosis&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProperty WithDiagnosis(Func<AnyEnergyPerformanceDiagnosis, AnyEnergyPerformanceDiagnosis> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProperty copy = new AnyProperty(this);

        copy._diagnosisRecipe = configure(_diagnosisRecipe);
        copy._diagnosis       = copy._diagnosisRecipe;

        return copy;
    }

    /// <summary>Pins <c>documents</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProperty WithDocuments(IReadOnlyList<DocumentLink> value) {
        AnyProperty copy = new AnyProperty(this);

        copy._documents = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>documents</c> is drawn from — <c>AnyValidDocuments()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;DocumentLink&gt;, AnyList&lt;DocumentLink&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProperty WithDocuments(Func<AnyList<DocumentLink>, AnyList<DocumentLink>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProperty copy = new AnyProperty(this);

        copy._documentsRecipe = configure(_documentsRecipe);
        copy._documents       = copy._documentsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Property" />.</summary>
    public Property Generate() {
        return new Property(_parcel.Generate(),
                            _kind.Generate(),
                            _livingArea.Generate(),
                            _diagnosis.Generate(),
                            _documents.Generate());
    }

}
