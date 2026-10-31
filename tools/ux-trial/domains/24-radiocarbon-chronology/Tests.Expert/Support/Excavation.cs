using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

/// <summary>Recipes for the arbitrary parts of an excavation, once the test has said what matters.</summary>
public static class Excavation {

    /// <summary>A site read against the straight curve; its name is arbitrary.</summary>
    public static SiteChronology SiteOnTheStraightCurve() {
        return Any.SiteChronology().WithCurve(Curves.Straight()).Generate();
    }

    /// <summary>A layer with the given code; its description is arbitrary.</summary>
    public static StratigraphicLayer Layer(string code) {
        return Any.StratigraphicLayer().WithCode(code).Generate();
    }

    /// <summary>
    /// One sample per measured age, under laboratory codes that differ from one another, since a site
    /// refuses the same code twice. Material and code are arbitrary; no reservoir offset applies.
    /// </summary>
    public static IReadOnlyList<Sample> SamplesMeasuredAt(params RadiocarbonAge[] ages) {
        return SamplesMeasuredAt(ages, labCodesInUse: []);
    }

    private static IReadOnlyList<Sample> SamplesMeasuredAt(RadiocarbonAge[] ages, IEnumerable<string> labCodesInUse) {
        string[]   inUse          = labCodesInUse.ToArray();
        // Workaround: Except refuses an empty list ("At least one value is required."), although
        // excluding nothing is a no-op, so the first layer of a site has to skip the call.
        AnyPattern anyFreeLabCode = inUse.Length == 0 ? AnySample.LabCodes() : AnySample.LabCodes().Except(inUse);
        HashSet<string> anyLabCodes    = Any.SetOf(anyFreeLabCode).WithCount(ages.Length).Generate();

        return anyLabCodes.Zip(ages, (labCode, age) => Any.Sample().WithLabCode(labCode).WithMeasurement(age).Generate())
                          .ToList();
    }

    /// <summary>Records a layer and attaches one sample per measured age to it.</summary>
    public static void AddDatedLayer(this SiteChronology site, string code, params RadiocarbonAge[] ages) {
        site.AddLayer(Layer(code));
        IEnumerable<string> labCodesInUse = site.Layers.SelectMany(layer => layer.Samples).Select(sample => sample.LabCode);
        foreach (Sample sample in SamplesMeasuredAt(ages, labCodesInUse.ToList())) {
            site.AttachSample(code, sample);
        }
    }

}
