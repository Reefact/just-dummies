// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DocumentLink --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DocumentLink" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDocumentLink : IAny<DocumentLink> {

    private IAny<DocumentKind> _kind;
    private IAny<Uri>          _location;
    private IAny<DateTime>     _uploadedAtUtc;

    private AnyEnum<DocumentKind> _kindRecipe;
    private AnyWebUri             _locationRecipe;
    private AnyDateTime           _uploadedAtUtcRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDocumentLink()
        : this(kind:          AnyValidKind(),
               location:      AnyValidLocation(),
               uploadedAtUtc: AnyValidUploadedAtUtc()) { }

    private static AnyEnum<DocumentKind> AnyValidKind() {
        return Any.Enum<DocumentKind>();
    }

    private static AnyWebUri AnyValidLocation() {
        return Any.Uri().Web().UsingHttps();
    }

    private static AnyDateTime AnyValidUploadedAtUtc() {
        // Draws are Kind.Utc.
        return Any.DateTime();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDocumentLink(AnyEnum<DocumentKind> kind,
                            AnyWebUri             location,
                            AnyDateTime           uploadedAtUtc) {
        _kind                = kind;
        _kindRecipe          = kind;
        _location            = location;
        _locationRecipe      = location;
        _uploadedAtUtc       = uploadedAtUtc;
        _uploadedAtUtcRecipe = uploadedAtUtc;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDocumentLink(AnyDocumentLink from) {
        _kind                = from._kind;
        _kindRecipe          = from._kindRecipe;
        _location            = from._location;
        _locationRecipe      = from._locationRecipe;
        _uploadedAtUtc       = from._uploadedAtUtc;
        _uploadedAtUtcRecipe = from._uploadedAtUtcRecipe;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDocumentLink WithKind(DocumentKind value) {
        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;DocumentKind&gt;, AnyEnum&lt;DocumentKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDocumentLink WithKind(Func<AnyEnum<DocumentKind>, AnyEnum<DocumentKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>location</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDocumentLink WithLocation(Uri value) {
        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._location = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>location</c> is drawn from — <c>AnyValidLocation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWebUri, AnyWebUri&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDocumentLink WithLocation(Func<AnyWebUri, AnyWebUri> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._locationRecipe = configure(_locationRecipe);
        copy._location       = copy._locationRecipe;

        return copy;
    }

    /// <summary>Pins <c>uploadedAtUtc</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDocumentLink WithUploadedAtUtc(DateTime value) {
        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._uploadedAtUtc = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>uploadedAtUtc</c> is drawn from — <c>AnyValidUploadedAtUtc()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDocumentLink WithUploadedAtUtc(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDocumentLink copy = new AnyDocumentLink(this);

        copy._uploadedAtUtcRecipe = configure(_uploadedAtUtcRecipe);
        copy._uploadedAtUtc       = copy._uploadedAtUtcRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DocumentLink" />.</summary>
    public DocumentLink Generate() {
        return new DocumentLink(_kind.Generate(),
                                _location.Generate(),
                                _uploadedAtUtc.Generate());
    }

}
