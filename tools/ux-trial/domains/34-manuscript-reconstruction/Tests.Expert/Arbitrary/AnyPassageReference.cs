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

    private readonly IAny<int> _book;
    private readonly IAny<int> _chapter;
    private readonly IAny<int> _line;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPassageReference()
        : this(book:    AnyValidBook(),
               chapter: AnyValidChapter(),
               line:    AnyValidLine()) { }

    private static AnyInt32 AnyValidBook() {
        return Any.Int32().Positive().LessThanOrEqualTo(99);
    }

    private static AnyInt32 AnyValidChapter() {
        return Any.Int32().Positive().LessThanOrEqualTo(999);
    }

    private static AnyInt32 AnyValidLine() {
        return Any.Int32().Positive().LessThanOrEqualTo(9999);
    }

    private AnyPassageReference(IAny<int> book,
                                IAny<int> chapter,
                                IAny<int> line) {
        _book    = book;
        _chapter = chapter;
        _line    = line;
    }

    /// <summary>Pins <c>book</c> to a fixed value.</summary>
    public AnyPassageReference WithBook(int value) {
        return new AnyPassageReference(new FixedValue<int>(value), _chapter, _line);
    }

    /// <summary>Draws <c>book</c> from <c>AnyValidBook()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPassageReference WithBook(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPassageReference(configure(AnyValidBook()), _chapter, _line);
    }

    /// <summary>Pins <c>chapter</c> to a fixed value.</summary>
    public AnyPassageReference WithChapter(int value) {
        return new AnyPassageReference(_book, new FixedValue<int>(value), _line);
    }

    /// <summary>Draws <c>chapter</c> from <c>AnyValidChapter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPassageReference WithChapter(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPassageReference(_book, configure(AnyValidChapter()), _line);
    }

    /// <summary>Pins <c>line</c> to a fixed value.</summary>
    public AnyPassageReference WithLine(int value) {
        return new AnyPassageReference(_book, _chapter, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>line</c> from <c>AnyValidLine()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPassageReference WithLine(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPassageReference(_book, _chapter, configure(AnyValidLine()));
    }

    /// <summary>Produces one arbitrary <see cref="PassageReference" />.</summary>
    public PassageReference Generate() {
        return new PassageReference(_book.Generate(),
                                    _chapter.Generate(),
                                    _line.Generate());
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
