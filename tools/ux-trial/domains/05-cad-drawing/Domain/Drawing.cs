namespace Drafting.Domain;

/// <summary>A drawing sheet moving through check, approval and release.</summary>
public sealed class Drawing {

    public static readonly DateOnly EarliestIssue = new(1990, 1, 1);

    private readonly List<Dimension> _dimensions;

    public Drawing(DrawingReference reference, string title, DrawingScale scale, ProjectionMethod projection, DateOnly issuedOn, IReadOnlyList<Dimension> dimensions) {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(scale);
        if (!Enum.IsDefined(projection)) {
            throw new ArgumentOutOfRangeException(nameof(projection), projection, "Unknown projection method.");
        }
        if (issuedOn < EarliestIssue) {
            throw new ArgumentOutOfRangeException(nameof(issuedOn), issuedOn, "Drawings issued before 1990 live in the paper archive.");
        }
        ArgumentNullException.ThrowIfNull(dimensions);

        Reference   = reference;
        Title       = title;
        Scale       = scale;
        Projection  = projection;
        IssuedOn    = issuedOn;
        _dimensions = dimensions.ToList();
        Status      = DrawingStatus.Draft;
    }

    public DrawingReference        Reference  { get; private set; }
    public string                  Title      { get; }
    public DrawingScale            Scale      { get; }
    public ProjectionMethod        Projection { get; }
    public DateOnly                IssuedOn   { get; }
    public DrawingStatus           Status     { get; private set; }
    public DateOnly?               ReleasedOn { get; private set; }
    public IReadOnlyList<Dimension> Dimensions => _dimensions;

    public void Check() {
        if (Status != DrawingStatus.Draft) {
            throw new InvalidOperationException($"Only a draft can be checked; {Reference} is {Status}.");
        }
        if (_dimensions.Count == 0) {
            throw new InvalidOperationException("A drawing without a single dimension cannot be checked.");
        }
        Status = DrawingStatus.Checked;
    }

    public void Approve() {
        if (Status != DrawingStatus.Checked) {
            throw new InvalidOperationException($"Only a checked drawing can be approved; {Reference} is {Status}.");
        }
        Status = DrawingStatus.Approved;
    }

    public void Release(DateOnly on) {
        if (Status != DrawingStatus.Approved) {
            throw new InvalidOperationException($"Only an approved drawing can be released; {Reference} is {Status}.");
        }
        if (on < IssuedOn) {
            throw new ArgumentOutOfRangeException(nameof(on), on, "A drawing cannot be released before it was issued.");
        }
        Status     = DrawingStatus.Released;
        ReleasedOn = on;
    }

    /// <summary>Opens the next revision of a released drawing, back to draft.</summary>
    public void Revise() {
        if (Status != DrawingStatus.Released) {
            throw new InvalidOperationException($"Only a released drawing is revised; {Reference} is {Status}.");
        }
        Reference  = Reference.NextRevision();
        Status     = DrawingStatus.Draft;
        ReleasedOn = null;
    }

}
