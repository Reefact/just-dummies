namespace PulmonaryNodules.Domain;

/// <summary>Two exams could not be brought into a common coordinate frame.</summary>
public sealed class RegistrationFailedException : InvalidOperationException
{
    public RegistrationFailedException(string message)
        : base(message)
    {
    }
}

/// <summary>A segmentation could not be recognised as the tracked nodule.</summary>
public sealed class NoduleMismatchException : InvalidOperationException
{
    public NoduleMismatchException(string message)
        : base(message)
    {
    }
}
