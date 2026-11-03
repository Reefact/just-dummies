namespace Laboratory.Tests;

/// <summary>
///     The generators are test fixtures written by hand where the scaffold could not read a rule; these cases keep them honest:
///     a thousand draws of each must construct, since a generator that sometimes throws turns every test using it into a flaky one.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 1_000;

    [Fact]
    public void Every_generated_domain_value_satisfies_its_own_constructor() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.SampleId().Generate();
            Any.AnalyteCode().Generate();
            Any.MeasurementUnit().Generate();
            Any.DilutionFactor().Generate();
            Any.Measurement().Generate();
            Any.ReferenceRange().Generate();
            Any.SpecimenQuality().Generate();
            Any.Specimen().Generate();
            Any.LabResult().Generate();
            Any.LabReport().Generate();
        }
    }

    [Fact]
    public void A_generated_range_sometimes_has_critical_limits_and_sometimes_not() {
        // Arrange
        List<ReferenceRange> ranges = Enumerable.Range(0, Draws).Select(_ => Any.ReferenceRange().Generate()).ToList();

        // Assert
        Assert.Contains(ranges, range => range.CriticalLow is null);
        Assert.Contains(ranges, range => range.CriticalLow is not null);
        Assert.Contains(ranges, range => range.CriticalHigh is null);
        Assert.Contains(ranges, range => range.CriticalHigh is not null);
    }

    [Fact]
    public void A_generated_report_holds_between_one_and_five_results() {
        // Arrange
        List<int> counts = Enumerable.Range(0, Draws).Select(_ => Any.LabReport().Generate().Results.Count).ToList();

        // Assert
        Assert.All(counts, count => Assert.InRange(count, 1, 5));
        Assert.Contains(1, counts);
        Assert.Contains(5, counts);
    }

}
