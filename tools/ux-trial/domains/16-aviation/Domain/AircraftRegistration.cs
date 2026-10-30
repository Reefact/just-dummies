using System.Text.RegularExpressions;

namespace Aviation.Domain;

/// <summary>
/// An aircraft registration mark: a national prefix, a hyphen and a suffix of letters (F-GKXA, D-AIBL, G-EUPT),
/// or the United States' N-number form without a hyphen (N12345, N123AB).
/// </summary>
public sealed class AircraftRegistration {

    private static readonly Regex Hyphenated = new Regex("^[A-Z]{1,2}-[A-Z]{3,4}$");
    private static readonly Regex NNumber    = new Regex("^N[1-9][0-9]{0,4}[A-Z]{0,2}$");

    public AircraftRegistration(string mark) {
        bool valid = mark switch {
            null                    => false,
            { Length: > 7 }         => false,
            _ when mark[0] == 'N'   => NNumber.IsMatch(mark),
            _                       => Hyphenated.IsMatch(mark)
        };
        if (!valid) {
            throw new ArgumentException($"'{mark}' is not an aircraft registration mark.", nameof(mark));
        }

        Mark = mark!;
    }

    public string Mark { get; }

    public bool IsUnitedStates => Mark[0] == 'N';

    public override string ToString() => Mark;

}
