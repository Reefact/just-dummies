using System.Text.RegularExpressions;

namespace Aviation.Domain;

/// <summary>A flight designator: a two-character airline code followed by one to four digits, such as AF1234.</summary>
public sealed class FlightNumber {

    public FlightNumber(string airlineDesignator, int number) {
        Validate(airlineDesignator, number);

        AirlineDesignator = airlineDesignator;
        Number            = number;
    }

    public string AirlineDesignator { get; }

    public int Number { get; }

    public override string ToString() => $"{AirlineDesignator}{Number}";

    private static void Validate(string airlineDesignator, int number) {
        if (airlineDesignator is null || !Regex.IsMatch(airlineDesignator, "^[A-Z0-9]{2}$")) {
            throw new ArgumentException("An airline designator is two upper-case letters or digits.", nameof(airlineDesignator));
        }

        if (number < 1 || number > 9999) {
            throw new ArgumentOutOfRangeException(nameof(number), number, "A flight number runs from 1 to 9999.");
        }
    }

}
