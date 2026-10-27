// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Course --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Course" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCourse : IAny<Course> {

    private readonly IAny<string> _code;
    private readonly IAny<string> _title;
    private readonly IAny<int>    _credits;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCourse()
        : this(code:    AnyValidCode(),
               title:   AnyValidTitle(),
               credits: AnyValidCredits()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{3}[0-9]{3}");
    }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidCredits() {
        return Any.Int32().Positive().LessThanOrEqualTo(30);
    }

    private AnyCourse(IAny<string> code,
                      IAny<string> title,
                      IAny<int>    credits) {
        _code    = code;
        _title   = title;
        _credits = credits;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyCourse WithCode(string value) {
        return new AnyCourse(new FixedValue<string>(value), _title, _credits);
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCourse WithCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCourse(configure(AnyValidCode()), _title, _credits);
    }

    /// <summary>Pins <c>title</c> to a fixed value.</summary>
    public AnyCourse WithTitle(string value) {
        return new AnyCourse(_code, new FixedValue<string>(value), _credits);
    }

    /// <summary>Draws <c>title</c> from <c>AnyValidTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCourse WithTitle(Func<AnyString, AnyString> configure) {
        return new AnyCourse(_code, configure(AnyValidTitle()), _credits);
    }

    /// <summary>Pins <c>credits</c> to a fixed value.</summary>
    public AnyCourse WithCredits(int value) {
        return new AnyCourse(_code, _title, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>credits</c> from <c>AnyValidCredits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCourse WithCredits(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCourse(_code, _title, configure(AnyValidCredits()));
    }

    /// <summary>Produces one arbitrary <see cref="Course" />.</summary>
    public Course Generate() {
        return new Course(_code.Generate(),
                          _title.Generate(),
                          _credits.Generate());
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
