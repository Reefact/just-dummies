using JustDummies;

namespace RoboticMilking.Domain;

// Written by hand, beside the scaffolded file: survives `any generate MilkFlowReading --force`.
public sealed partial class AnyMilkFlowReading
{
    /// <summary>A reading from a healthy quarter: no mastitis signal, no blood. Its milk goes to the tank.</summary>
    public AnyMilkFlowReading Healthy()
    {
        return WithConductivityMsPerCm(c => c.LessThanOrEqualTo(MilkFlowReading.MastitisConductivityThreshold))
              .WithBloodDetected(false);
    }
}
