using System;

using JustDummies;

namespace Clinical.Domain;

public sealed partial class AnyNir {

    private string _sexDigit   = "[12]";
    private string _yearDigits = "[0-9]{2}";
    private string _monthDigits = "(0[1-9]|1[0-2])";

    /// <summary>A NIR whose year and month digits are the given ones, key included.</summary>
    public AnyNir BornIn(int year, int month) {
        AnyNir copy = Copy();

        copy._yearDigits  = $"{year % 100:00}";
        copy._monthDigits = $"{month:00}";

        return copy.Rebuilt();
    }

    /// <summary>A NIR of the given sex, key included.</summary>
    public AnyNir OfSex(Sex sex) {
        AnyNir copy = Copy();

        copy._sexDigit = sex == Sex.Male ? "1" : "2";

        return copy.Rebuilt();
    }

    private AnyNir Copy() {
        AnyNir copy = new AnyNir(this);

        copy._sexDigit    = _sexDigit;
        copy._yearDigits  = _yearDigits;
        copy._monthDigits = _monthDigits;

        return copy;
    }

    private AnyNir Rebuilt() {
        _valueRecipe = BodyWith(Any.StringMatching(_sexDigit + _yearDigits + _monthDigits + "([0-9]{2}|2[AB])[0-9]{6}"));
        _value       = _valueRecipe;

        return this;
    }

}
