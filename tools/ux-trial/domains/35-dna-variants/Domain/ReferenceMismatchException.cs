namespace DnaVariants.Domain;

/// <summary>A variant does not agree with the reference it is described against.</summary>
public sealed class ReferenceMismatchException : Exception
{
    public ReferenceMismatchException()
    {
    }

    public ReferenceMismatchException(string message)
        : base(message)
    {
    }

    public ReferenceMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
