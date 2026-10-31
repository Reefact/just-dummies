namespace RoboticMilking.Domain;

/// <summary>Thrown when the robot refuses an operation the current state of the session forbids.</summary>
public sealed class MilkingRefusedException : InvalidOperationException
{
    public MilkingRefusedException(string message)
        : base(message)
    {
    }
}
