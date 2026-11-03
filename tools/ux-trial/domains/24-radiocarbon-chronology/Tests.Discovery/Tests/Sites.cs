namespace RadiocarbonChronology.Tests.Discovery;

/// <summary>Builds the sites the modelling tests are about: the layers' codes, the ages of their samples and the curve are what the expected estimates come from.</summary>
internal static class Sites {

    public static SiteChronology OnLinearCurve() {
        return Any.SiteChronology().WithCurve(Curves.Linear()).Generate();
    }

    /// <summary>Adds a layer of that code, dated by one charcoal sample per age (each measured to 30 years).</summary>
    public static void AddDatedLayer(SiteChronology site, string code, params int[] yearsBp) {
        AddLayerMeasuredTo(site, code, 30, yearsBp);
    }

    /// <summary>The same, with the laboratory uncertainty of every sample given.</summary>
    public static void AddLayerMeasuredTo(SiteChronology site, string code, int uncertainty, params int[] yearsBp) {
        site.AddLayer(Any.StratigraphicLayer().WithCode(code).Generate());
        foreach (int measured in yearsBp) {
            site.AttachSample(code, Any.Sample().WithMaterial(SampleMaterial.Charcoal).WithMeasurement(new RadiocarbonAge(measured, uncertainty)).Generate());
        }
    }

    public static void Precedes(SiteChronology site, string olderLayerCode, string youngerLayerCode) {
        site.AddRelation(new PrecedenceRelation(olderLayerCode, youngerLayerCode));
    }

}
