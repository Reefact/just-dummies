// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Query --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Query" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuery : IAny<Query> {

    private IAny<int>      _number;
    private IAny<string>   _fieldName;
    private IAny<string>   _text;
    private IAny<DateOnly> _raisedOn;

    private AnyInt32    _numberRecipe;
    private AnyPattern   _fieldNameRecipe;
    private AnyString   _textRecipe;
    private AnyDateOnly _raisedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuery()
        : this(number:    AnyValidNumber(),
               fieldName: AnyValidFieldName(),
               text:      AnyValidText(),
               raisedOn:  AnyValidRaisedOn()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Positive();
    }

    private static AnyPattern AnyValidFieldName() {
        return Any.StringMatching(@"[A-Z][A-Za-z0-9]{0,11}(\.[A-Z][A-Za-z0-9]{0,11}){0,2}");
    }

    private static AnyString AnyValidText() {
        return Any.String().NotBlank().WithMaxLength(Query.MaxTextLength);
    }

    private static AnyDateOnly AnyValidRaisedOn() {
        return TrialCalendar.Date();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyQuery(AnyInt32    number,
                     AnyPattern   fieldName,
                     AnyString   text,
                     AnyDateOnly raisedOn) {
        _number          = number;
        _numberRecipe    = number;
        _fieldName       = fieldName;
        _fieldNameRecipe = fieldName;
        _text            = text;
        _textRecipe      = text;
        _raisedOn        = raisedOn;
        _raisedOnRecipe  = raisedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyQuery(AnyQuery from) {
        _number          = from._number;
        _numberRecipe    = from._numberRecipe;
        _fieldName       = from._fieldName;
        _fieldNameRecipe = from._fieldNameRecipe;
        _text            = from._text;
        _textRecipe      = from._textRecipe;
        _raisedOn        = from._raisedOn;
        _raisedOnRecipe  = from._raisedOnRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuery WithNumber(int value) {
        AnyQuery copy = new AnyQuery(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuery WithNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuery copy = new AnyQuery(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>fieldName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuery WithFieldName(string value) {
        AnyQuery copy = new AnyQuery(this);

        copy._fieldName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fieldName</c> is drawn from — <c>AnyValidFieldName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuery WithFieldName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuery copy = new AnyQuery(this);

        copy._fieldNameRecipe = configure(_fieldNameRecipe);
        copy._fieldName       = copy._fieldNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>text</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuery WithText(string value) {
        AnyQuery copy = new AnyQuery(this);

        copy._text = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>text</c> is drawn from — <c>AnyValidText()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuery WithText(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuery copy = new AnyQuery(this);

        copy._textRecipe = configure(_textRecipe);
        copy._text       = copy._textRecipe;

        return copy;
    }

    /// <summary>Pins <c>raisedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuery WithRaisedOn(DateOnly value) {
        AnyQuery copy = new AnyQuery(this);

        copy._raisedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>raisedOn</c> is drawn from — <c>AnyValidRaisedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuery WithRaisedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuery copy = new AnyQuery(this);

        copy._raisedOnRecipe = configure(_raisedOnRecipe);
        copy._raisedOn       = copy._raisedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Query" />.</summary>
    public Query Generate() {
        return new Query(_number.Generate(),
                         _fieldName.Generate(),
                         _text.Generate(),
                         _raisedOn.Generate());
    }

}
