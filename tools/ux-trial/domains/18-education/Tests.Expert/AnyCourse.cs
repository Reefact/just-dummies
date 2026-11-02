// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Course --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Course" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCourse : IAny<Course> {

    private IAny<string> _code;
    private IAny<string> _title;
    private IAny<int>    _credits;

    private AnyPattern _codeRecipe;
    private AnyString _titleRecipe;
    private AnyInt32  _creditsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCourse()
        : this(code:    AnyValidCode(),
               title:   AnyValidTitle(),
               credits: AnyValidCredits()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("^[A-Z]{3}[0-9]{3}$");
    }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidCredits() {
        return Any.Int32().Positive().LessThanOrEqualTo(30);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCourse(AnyPattern code,
                      AnyString title,
                      AnyInt32  credits) {
        _code          = code;
        _codeRecipe    = code;
        _title         = title;
        _titleRecipe   = title;
        _credits       = credits;
        _creditsRecipe = credits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCourse(AnyCourse from) {
        _code          = from._code;
        _codeRecipe    = from._codeRecipe;
        _title         = from._title;
        _titleRecipe   = from._titleRecipe;
        _credits       = from._credits;
        _creditsRecipe = from._creditsRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCourse WithCode(string value) {
        AnyCourse copy = new AnyCourse(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCourse WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCourse copy = new AnyCourse(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCourse WithTitle(string value) {
        AnyCourse copy = new AnyCourse(this);

        copy._title = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCourse WithTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCourse copy = new AnyCourse(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;

        return copy;
    }

    /// <summary>Pins <c>credits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCourse WithCredits(int value) {
        AnyCourse copy = new AnyCourse(this);

        copy._credits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>credits</c> is drawn from — <c>AnyValidCredits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCourse WithCredits(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCourse copy = new AnyCourse(this);

        copy._creditsRecipe = configure(_creditsRecipe);
        copy._credits       = copy._creditsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Course" />.</summary>
    public Course Generate() {
        return new Course(_code.Generate(),
                          _title.Generate(),
                          _credits.Generate());
    }

}
