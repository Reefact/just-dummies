using CaptiveBreeding.Domain;

namespace CaptiveBreeding.Tests.Expert.Scenarios;

/// <summary>Three generations: two grandparents, their son and daughter, each mated outside the family.</summary>
internal sealed class Family {

    private Family(Studbook studbook, Individual grandsire, Individual granddam, Individual cousin, Individual otherCousin) {
        Studbook    = studbook;
        Grandsire   = grandsire;
        Granddam    = granddam;
        Cousin      = cousin;
        OtherCousin = otherCousin;
    }

    public Studbook   Studbook    { get; }
    public Individual Grandsire   { get; }
    public Individual Granddam    { get; }
    public Individual Cousin      { get; }
    public Individual OtherCousin { get; }

    public static Family WithFirstCousins() {
        Studbook   studbook  = new();
        Individual grandsire = studbook.Male();
        Individual granddam  = studbook.Female();
        Individual son       = studbook.SonOf(grandsire, granddam);
        Individual daughter  = studbook.DaughterOf(grandsire, granddam);
        Individual cousin    = studbook.OffspringOf(son, studbook.Female());
        Individual other     = studbook.OffspringOf(studbook.Male(), daughter);

        return new Family(studbook, grandsire, granddam, cousin, other);
    }

}
