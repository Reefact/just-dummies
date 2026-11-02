// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Booking --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Booking" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBooking : IAny<Booking> {

    private AnyOrderedPair<DateTime> _startAndEnd;
    private IAny<string>             _note;

    private AnyDateTime _startRecipe;
    private AnyDateTime _endRecipe;
    private AnyString   _noteRecipe;

    private DateTime? _startPin;
    private DateTime? _endPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBooking()
        : this(start: AnyValidStart(),
               end:   AnyValidEnd(),
               note:  AnyValidNote()) { }

    private static AnyDateTime AnyValidStart() {
        return Any.DateTime();
    }

    private static AnyDateTime AnyValidEnd() {
        return Any.DateTime();
    }

    private static AnyString AnyValidNote() {
        return Any.String();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBooking(AnyDateTime start,
                       AnyDateTime end,
                       AnyString   note) {
        _startRecipe = start;
        _endRecipe   = end;
        _note        = note;
        _noteRecipe  = note;
        _startAndEnd = StartAndEnd();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBooking(AnyBooking from) {
        _startAndEnd = from._startAndEnd;
        _startRecipe = from._startRecipe;
        _startPin    = from._startPin;
        _endRecipe   = from._endRecipe;
        _endPin      = from._endPin;
        _note        = from._note;
        _noteRecipe  = from._noteRecipe;
    }

    /// <summary>Draws <c>start</c> and <c>end</c> together, <c>start</c> never after <c>end</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<DateTime> StartAndEnd() {
        AnyOrderedPair<DateTime> pair = Any.OrderedPair(_startRecipe, _endRecipe);

        if (_startPin is { } start) { pair = pair.WithFirst(start); }
        if (_endPin is { } end) { pair = pair.WithSecond(end); }

        return pair;
    }

    /// <summary>Pins <c>start</c> to a fixed value, which bounds <c>end</c>; its recipe is kept for a later refinement.</summary>
    public AnyBooking WithStart(DateTime value) {
        AnyBooking copy = new AnyBooking(this);

        copy._startPin    = value;
        copy._startAndEnd = copy.StartAndEnd();

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>end</c>.</summary>
    public AnyBooking WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        AnyBooking copy = new AnyBooking(this);

        copy._startRecipe = configure(_startRecipe);
        copy._startPin    = null;
        copy._startAndEnd = copy.StartAndEnd();

        return copy;
    }

    /// <summary>Pins <c>end</c> to a fixed value, which bounds <c>start</c>; its recipe is kept for a later refinement.</summary>
    public AnyBooking WithEnd(DateTime value) {
        AnyBooking copy = new AnyBooking(this);

        copy._endPin      = value;
        copy._startAndEnd = copy.StartAndEnd();

        return copy;
    }

    /// <summary>Refines the recipe <c>end</c> is drawn from — <c>AnyValidEnd()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>start</c>.</summary>
    public AnyBooking WithEnd(Func<AnyDateTime, AnyDateTime> configure) {
        AnyBooking copy = new AnyBooking(this);

        copy._endRecipe   = configure(_endRecipe);
        copy._endPin      = null;
        copy._startAndEnd = copy.StartAndEnd();

        return copy;
    }

    /// <summary>Pins <c>note</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyBooking WithNote(string value) {
        AnyBooking copy = new AnyBooking(this);

        copy._note = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>note</c> is drawn from — <c>AnyValidNote()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyBooking WithNote(Func<AnyString, AnyString> configure) {
        AnyBooking copy = new AnyBooking(this);

        copy._noteRecipe = configure(_noteRecipe);
        copy._note       = copy._noteRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Booking" />.</summary>
    public Booking Generate() {
        (DateTime start, DateTime end) = _startAndEnd.Generate();

        return new Booking(start,
                           end,
                           _note.Generate());
    }

}
