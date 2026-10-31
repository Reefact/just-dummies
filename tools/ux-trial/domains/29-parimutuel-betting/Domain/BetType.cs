namespace ParimutuelBetting.Domain;

/// <summary>The bet types a pool can be opened for.</summary>
public enum BetType
{
    /// <summary>One horse, must finish first.</summary>
    Win,

    /// <summary>One horse, must finish within the places the pool pays.</summary>
    Place,

    /// <summary>Two horses, first and second in either order.</summary>
    Quinella,

    /// <summary>Two horses, first and second in exact order.</summary>
    Exacta,

    /// <summary>Three horses, first, second and third in exact order.</summary>
    Trifecta,
}

/// <summary>The combination rules attached to each bet type.</summary>
public static class BetTypeRules
{
    /// <summary>The number of horses a combination of this bet type names.</summary>
    public static int Arity(this BetType betType) => betType switch
    {
        BetType.Win => 1,
        BetType.Place => 1,
        BetType.Quinella => 2,
        BetType.Exacta => 2,
        BetType.Trifecta => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unknown bet type."),
    };

    /// <summary>Whether the order of the horses in a combination matters.</summary>
    public static bool IsOrdered(this BetType betType) => betType switch
    {
        BetType.Win => true,
        BetType.Place => true,
        BetType.Quinella => false,
        BetType.Exacta => true,
        BetType.Trifecta => true,
        _ => throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unknown bet type."),
    };
}
