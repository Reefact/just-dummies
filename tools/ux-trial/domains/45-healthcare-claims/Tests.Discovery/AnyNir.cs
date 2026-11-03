using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>An arbitrary <see cref="Nir" />: body drawn, control key computed by the domain's own <see cref="Nir.ComputeKey" />.</summary>
public sealed partial class AnyNir : IAny<Nir> {

    private IAny<string> _sex;
    private IAny<string> _birthYear;
    private IAny<string> _birthMonth;
    private IAny<string> _place;
    private IAny<string> _rest;

    public AnyNir() {
        _sex        = Any.StringMatching("[12]");
        _birthYear  = Any.StringMatching("[0-9]{2}");
        _birthMonth = Any.StringMatching("0[1-9]|1[0-2]");
        _place      = Any.StringMatching("[0-9]{2}|2[AB]");
        _rest       = Any.StringMatching("[0-9]{6}");
    }

    private AnyNir(AnyNir from) {
        _sex        = from._sex;
        _birthYear  = from._birthYear;
        _birthMonth = from._birthMonth;
        _place      = from._place;
        _rest       = from._rest;
    }

    /// <summary>The NIR carries the birth year (two digits) and month: this ties it to a birth date.</summary>
    public AnyNir BornOn(DateOnly birthDate) {
        AnyNir copy = new AnyNir(this);

        copy._birthYear  = Any.Fixed((birthDate.Year % 100).ToString("00"));
        copy._birthMonth = Any.Fixed(birthDate.Month.ToString("00"));

        return copy;
    }

    public Nir Generate() {
        string body = _sex.Generate() + _birthYear.Generate() + _birthMonth.Generate() + _place.Generate() + _rest.Generate();

        return new Nir(body + Nir.ComputeKey(body).ToString("00"));
    }

}
