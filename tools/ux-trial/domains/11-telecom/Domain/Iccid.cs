using System.Text.RegularExpressions;

namespace Telecom.Domain;

/// <summary>A SIM card serial number: the telecom industry prefix 89, then 17 or 18 digits.</summary>
public sealed class Iccid {

    public Iccid(string value) {
        if (!Regex.IsMatch(value, @"^89[0-9]{17,18}$")) {
            throw new ArgumentException($"'{value}' is not an ICCID.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string CountryCode => Value.Substring(2, 2);

}
