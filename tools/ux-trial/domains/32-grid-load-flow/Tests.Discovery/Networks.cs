using JustDummies;

namespace GridLoadFlow.Domain.Tests;

/// <summary>Small hand-built networks whose topology and electrical values are what the tests are about.</summary>
internal static class Networks {

    public static Bus Slack(string id, double baseKv = 110.0, double setpoint = 1.0, PowerInjection? load = null) {
        return new Bus(new BusId(id), "slack " + id, BusType.Slack, baseKv, PowerInjection.Zero, load ?? PowerInjection.Zero, setpoint, VoltageLimits.Standard);
    }

    public static Bus Load(string id, double activeMw, double reactiveMvar, double baseKv = 110.0) {
        return new Bus(new BusId(id), "load " + id, BusType.Pq, baseKv, PowerInjection.Zero, new PowerInjection(activeMw, reactiveMvar), null, VoltageLimits.Standard);
    }

    public static Bus Generator(string id, double activeMw, double setpoint, double baseKv = 110.0) {
        return new Bus(new BusId(id), "generator " + id, BusType.Pv, baseKv, new PowerInjection(activeMw, 0.0), PowerInjection.Zero, setpoint, VoltageLimits.Standard);
    }

    public static Line Line(string id, string from, string to, double resistance = 0.02, double reactance = 0.08, double charging = 0.0, double rating = 500.0) {
        return new Line(id, new BusId(from), new BusId(to), new Impedance(resistance, reactance), charging, rating);
    }

    public static Transformer Transformer(string id, string hv, string lv, double tap = 1.0, double reactance = 0.1, double rating = 500.0) {
        return new Transformer(id, new BusId(hv), new BusId(lv), new Impedance(0.005, reactance), rating, tap);
    }

    /// <summary>S - A - B - S ring: three buses, three lines; any one line can be opened without islanding.</summary>
    public static GridNetwork Ring() {
        return new GridNetwork("ring", 100.0,
                               new[] { Slack("S"), Load("A", 40.0, 10.0), Load("B", 30.0, 5.0) },
                               new[] { Line("SA", "S", "A"), Line("AB", "A", "B"), Line("BS", "B", "S") },
                               new List<Transformer>());
    }

    /// <summary>S - A - B in a chain: every line is the only way to reach the buses behind it.</summary>
    public static GridNetwork Chain() {
        return new GridNetwork("chain", 100.0,
                               new[] { Slack("S"), Load("A", 40.0, 10.0), Load("B", 30.0, 5.0) },
                               new[] { Line("SA", "S", "A"), Line("AB", "A", "B") },
                               new List<Transformer>());
    }

}
