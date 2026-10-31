using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>Raised when the iterative solution does not settle within the allowed iterations.</summary>
public sealed class LoadFlowDivergedException : Exception
{
    public LoadFlowDivergedException(int iterations, double lastMismatchPu)
        : base($"Load flow did not converge after {iterations} iterations (last voltage update {lastMismatchPu:G3} pu).")
    {
        Iterations = iterations;
        LastMismatchPu = lastMismatchPu;
    }

    public int Iterations { get; }

    public double LastMismatchPu { get; }
}

/// <summary>Gauss-Seidel load flow with acceleration, on a per-unit admittance model.</summary>
public sealed class LoadFlowSolver
{
    public LoadFlowSolver(SolverSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
    }

    public SolverSettings Settings { get; }

    public LoadFlowResult Solve(GridNetwork network)
    {
        ArgumentNullException.ThrowIfNull(network);
        IReadOnlyList<BusId> order = network.BusOrder();
        int n = order.Count;
        Complex[,] y = network.BuildAdmittanceMatrix(order);
        Bus[] buses = order.Select(network.GetBus).ToArray();
        Complex[] v = buses.Select(b => new Complex(b.VoltageSetpointPu ?? 1.0, 0.0)).ToArray();
        Complex[] s = buses.Select(b => b.NetInjection.ToPerUnit(network.BaseMva)).ToArray();

        for (int iteration = 1; iteration <= Settings.MaximumIterations; iteration++)
        {
            double largestChange = 0.0;
            for (int i = 0; i < n; i++)
            {
                if (buses[i].Type == BusType.Slack)
                {
                    continue;
                }

                Complex sum = Complex.Zero;
                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        sum += y[i, k] * v[k];
                    }
                }

                if (buses[i].Type == BusType.Pv)
                {
                    double q = -(Complex.Conjugate(v[i]) * (sum + (y[i, i] * v[i]))).Imaginary;
                    s[i] = new Complex(s[i].Real, q);
                }

                Complex updated = ((Complex.Conjugate(s[i]) / Complex.Conjugate(v[i])) - sum) / y[i, i];
                updated = v[i] + (Settings.AccelerationFactor * (updated - v[i]));
                if (buses[i].Type == BusType.Pv)
                {
                    updated = Complex.FromPolarCoordinates(buses[i].VoltageSetpointPu!.Value, updated.Phase);
                }

                if (!double.IsFinite(updated.Real) || !double.IsFinite(updated.Imaginary))
                {
                    throw new LoadFlowDivergedException(iteration, double.PositiveInfinity);
                }

                largestChange = Math.Max(largestChange, (updated - v[i]).Magnitude);
                v[i] = updated;
            }

            if (largestChange < Settings.TolerancePu)
            {
                return BuildResult(network, order, y, v, iteration);
            }

            if (iteration == Settings.MaximumIterations)
            {
                throw new LoadFlowDivergedException(iteration, largestChange);
            }
        }

        throw new LoadFlowDivergedException(Settings.MaximumIterations, double.NaN);
    }

    private static LoadFlowResult BuildResult(GridNetwork network, IReadOnlyList<BusId> order, Complex[,] y, Complex[] v, int iterations)
    {
        Dictionary<BusId, int> index = order.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i);
        double baseMva = network.BaseMva;
        List<BranchFlow> flows = new();
        foreach (Line line in network.Lines.Where(l => l.InService))
        {
            flows.Add(Flow(line.Id, line.FromBus, line.ToBus, line.Stamp(), v[index[line.FromBus]], v[index[line.ToBus]], line.RatingMva, baseMva));
        }

        foreach (Transformer t in network.Transformers)
        {
            flows.Add(Flow(t.Id, t.HighVoltageBus, t.LowVoltageBus, t.Stamp(), v[index[t.HighVoltageBus]], v[index[t.LowVoltageBus]], t.RatingMva, baseMva));
        }

        int slack = index[network.SlackBus.Id];
        Complex current = Complex.Zero;
        for (int k = 0; k < order.Count; k++)
        {
            current += y[slack, k] * v[k];
        }

        Complex slackInjection = v[slack] * Complex.Conjugate(current) * baseMva;
        PowerInjection slackLoad = network.SlackBus.Load;
        PowerInjection slackGeneration = new(slackInjection.Real + slackLoad.ActiveMw, slackInjection.Imaginary + slackLoad.ReactiveMvar);
        Dictionary<BusId, Complex> voltages = order.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => v[p.i]);
        return new LoadFlowResult(iterations, voltages, flows, slackGeneration);
    }

    private static BranchFlow Flow(string id, BusId from, BusId to, (Complex Ff, Complex Ft, Complex Tf, Complex Tt) s, Complex vf, Complex vt, double rating, double baseMva)
    {
        Complex currentFrom = (s.Ff * vf) + (s.Ft * vt);
        Complex currentTo = (s.Tf * vf) + (s.Tt * vt);
        return new BranchFlow(id, from, to, vf * Complex.Conjugate(currentFrom) * baseMva, vt * Complex.Conjugate(currentTo) * baseMva, rating);
    }
}
