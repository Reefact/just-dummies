namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     An animal's identifier: two capital letters for the country, then twelve digits. The pattern is the whole
///     rule, which is why the strings and scaffolder pages use it to show a narrower pattern at one call site.
/// </summary>
public sealed class AnimalId {

    public AnimalId(string value) {
        Value = value;
    }

    /// <summary>The identifier, as written.</summary>
    public string Value { get; }

}

/// <summary>
///     A runner's number in a race: 1 to 20, whatever the size of the race it runs in. A record, because
///     "distinct" on a list of them has to mean distinct numbers — a class would compare by reference, and every
///     drawn instance would be distinct from every other.
/// </summary>
public sealed record HorseNumber {

    public HorseNumber(int value) {
        Value = value;
    }

    /// <summary>The number.</summary>
    public int Value { get; }

}

/// <summary>
///     A bet on several runners. It knows nothing of the race it is placed on — the size of the field is another
///     aggregate's fact — which is why a test that wants "three horses of a race of twelve" has to state that rule
///     itself.
/// </summary>
public sealed class Combination {

    public Combination(IReadOnlyList<HorseNumber> horses) {
        Horses = horses;
    }

    /// <summary>The runners.</summary>
    public IReadOnlyList<HorseNumber> Horses { get; }

}
