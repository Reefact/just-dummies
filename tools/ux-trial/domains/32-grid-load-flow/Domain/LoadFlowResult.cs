using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>A converged solution: complex bus voltages (pu), branch flows and the slack's balancing injection.</summary>
public sealed class LoadFlowResult
{
    private readonly Dictionary<BusId, Complex> voltages;

    public LoadFlowResult(int iterations, IReadOnlyDictionary<BusId, Complex> voltagesPu, IReadOnlyList<BranchFlow> branchFlows, PowerInjection slackGeneration)
    {
        ArgumentNullException.ThrowIfNull(voltagesPu);
        ArgumentNullException.ThrowIfNull(branchFlows);
        ArgumentNullException.ThrowIfNull(slackGeneration);
        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), iterations, "A result takes at least one iteration.");
        }

        if (voltagesPu.Count < 2)
        {
            throw new ArgumentException("A result covers at least two buses.", nameof(voltagesPu));
        }

        Iterations = iterations;
        voltages = new Dictionary<BusId, Complex>(voltagesPu);
        BranchFlows = branchFlows.ToList();
        SlackGeneration = slackGeneration;
    }

    public int Iterations { get; }

    public IReadOnlyDictionary<BusId, Complex> VoltagesPu => voltages;

    public IReadOnlyList<BranchFlow> BranchFlows { get; }

    public PowerInjection SlackGeneration { get; }

    public double TotalLossesMw => BranchFlows.Sum(f => f.LossesMw);

    public double VoltageMagnitudePu(BusId bus) => voltages[bus].Magnitude;

    public double VoltageAngleDegrees(BusId bus) => voltages[bus].Phase * 180.0 / Math.PI;

    /// <summary>Buses whose solved magnitude leaves their operating band.</summary>
    public IReadOnlyList<BusId> VoltageViolations(GridNetwork network)
    {
        ArgumentNullException.ThrowIfNull(network);
        return network.Buses.Where(b => voltages.ContainsKey(b.Id) && !b.Limits.Contains(voltages[b.Id].Magnitude)).Select(b => b.Id).ToList();
    }

    public IReadOnlyList<BranchFlow> OverloadedBranches => BranchFlows.Where(f => f.IsOverloaded).ToList();
}
