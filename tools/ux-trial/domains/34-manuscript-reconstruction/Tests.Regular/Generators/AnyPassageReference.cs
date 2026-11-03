// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PassageReference --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PassageReference" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPassageReference : IAny<PassageReference> {

    private IAny<int> _book;
    private IAny<int> _chapter;
    private IAny<int> _line;

    private AnyInt32 _bookRecipe;
    private AnyInt32 _chapterRecipe;
    private AnyInt32 _lineRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPassageReference()
        : this(book:    AnyValidBook(),
               chapter: AnyValidChapter(),
               line:    AnyValidLine()) { }

    private static AnyInt32 AnyValidBook() {
        return Any.Int32().Positive().LessThanOrEqualTo(PassageReference.MaxBook);
    }

    private static AnyInt32 AnyValidChapter() {
        return Any.Int32().Positive().LessThanOrEqualTo(PassageReference.MaxChapter);
    }

    private static AnyInt32 AnyValidLine() {
        return Any.Int32().Positive().LessThanOrEqualTo(PassageReference.MaxLine);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPassageReference(AnyInt32 book,
                                AnyInt32 chapter,
                                AnyInt32 line) {
        _book          = book;
        _bookRecipe    = book;
        _chapter       = chapter;
        _chapterRecipe = chapter;
        _line          = line;
        _lineRecipe    = line;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPassageReference(AnyPassageReference from) {
        _book          = from._book;
        _bookRecipe    = from._bookRecipe;
        _chapter       = from._chapter;
        _chapterRecipe = from._chapterRecipe;
        _line          = from._line;
        _lineRecipe    = from._lineRecipe;
    }

    /// <summary>Pins <c>book</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPassageReference WithBook(int value) {
        AnyPassageReference copy = new AnyPassageReference(this);

        copy._book = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>book</c> is drawn from — <c>AnyValidBook()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPassageReference WithBook(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPassageReference copy = new AnyPassageReference(this);

        copy._bookRecipe = configure(_bookRecipe);
        copy._book       = copy._bookRecipe;

        return copy;
    }

    /// <summary>Pins <c>chapter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPassageReference WithChapter(int value) {
        AnyPassageReference copy = new AnyPassageReference(this);

        copy._chapter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>chapter</c> is drawn from — <c>AnyValidChapter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPassageReference WithChapter(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPassageReference copy = new AnyPassageReference(this);

        copy._chapterRecipe = configure(_chapterRecipe);
        copy._chapter       = copy._chapterRecipe;

        return copy;
    }

    /// <summary>Pins <c>line</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPassageReference WithLine(int value) {
        AnyPassageReference copy = new AnyPassageReference(this);

        copy._line = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>line</c> is drawn from — <c>AnyValidLine()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPassageReference WithLine(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPassageReference copy = new AnyPassageReference(this);

        copy._lineRecipe = configure(_lineRecipe);
        copy._line       = copy._lineRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PassageReference" />.</summary>
    public PassageReference Generate() {
        return new PassageReference(_book.Generate(),
                                    _chapter.Generate(),
                                    _line.Generate());
    }

}
