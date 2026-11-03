using System;
using System.Globalization;

using Clinical.Domain;
using JustDummies;

namespace Clinical.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary well-formed <see cref="Nir" /> values: sex digit, birth year and month,
///     department (Corsica included), serial, and the control key computed from those. Scaffolded by
///     <c>any</c>, then rewritten by hand: a NIR is a body followed by a key computed from it, which no
///     per-parameter recipe can express. The key is computed here, not through <see cref="Nir.ComputeKey" />:
///     the tests of the key must not lean on the code they test.
/// </summary>
public sealed partial class AnyNir : IAny<Nir> {

    private readonly string?      _pinned;
    private readonly IAny<string> _sex;
    private readonly IAny<int>    _year;
    private readonly IAny<int>    _month;
    private readonly IAny<string> _department;
    private readonly IAny<string> _serial;

    public AnyNir()
        : this(null,
               Any.OneOf("1", "2"),
               Any.Int32().Between(0, 99),
               Any.Int32().Between(1, 12),
               Any.StringMatching("[0-9]{2}|2[AB]"),
               Any.StringMatching("[0-9]{6}")) { }

    private AnyNir(string? pinned, IAny<string> sex, IAny<int> year, IAny<int> month, IAny<string> department, IAny<string> serial) {
        _pinned     = pinned;
        _sex        = sex;
        _year       = year;
        _month      = month;
        _department = department;
        _serial     = serial;
    }

    /// <summary>Pins the whole NIR, key included, to a literal.</summary>
    public AnyNir WithValue(string value) {
        return new AnyNir(value, _sex, _year, _month, _department, _serial);
    }

    /// <summary>Draws a NIR of the given sex.</summary>
    public AnyNir OfSex(Sex sex) {
        return new AnyNir(_pinned, Any.Fixed(sex == Sex.Male ? "1" : "2"), _year, _month, _department, _serial);
    }

    /// <summary>Draws a NIR carrying the given two-digit birth year and birth month.</summary>
    public AnyNir BornIn(int twoDigitYear, int month) {
        return new AnyNir(_pinned, _sex, Any.Fixed(twoDigitYear), Any.Fixed(month), _department, _serial);
    }

    /// <summary>Draws a NIR whose year and month are those of the given birth date.</summary>
    public AnyNir BornOn(DateOnly birthDate) {
        return BornIn(birthDate.Year % 100, birthDate.Month);
    }

    /// <summary>Draws a NIR of a given department, e.g. <c>"75"</c> or <c>"2A"</c>.</summary>
    public AnyNir InDepartment(string department) {
        return new AnyNir(_pinned, _sex, _year, _month, Any.Fixed(department), _serial);
    }

    /// <summary>Produces one arbitrary <see cref="Nir" />.</summary>
    public Nir Generate() {
        if (_pinned is not null) {
            return new Nir(_pinned);
        }

        string body = _sex.Generate()
                    + _year.Generate().ToString("00", CultureInfo.InvariantCulture)
                    + _month.Generate().ToString("00", CultureInfo.InvariantCulture)
                    + _department.Generate()
                    + _serial.Generate();

        return new Nir(body + KeyOf(body).ToString("00", CultureInfo.InvariantCulture));
    }

    /// <summary>The control key of a 13-character body, written out from the rule rather than borrowed from the domain.</summary>
    public static int KeyOf(string body) {
        long number = long.Parse(body.Replace("2A", "19").Replace("2B", "18"), CultureInfo.InvariantCulture);

        return (int)(97 - number % 97);
    }

}
