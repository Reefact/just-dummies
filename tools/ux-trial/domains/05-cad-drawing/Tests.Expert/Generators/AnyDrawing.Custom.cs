using JustDummies;

namespace Drafting.Domain.Tests.Expert.Generators;

public sealed partial class AnyDrawing {

    /// <summary>
    ///     A drawing taken through the real transitions to <paramref name="status" />, with at least one dimension
    ///     so that it can be checked. The release, when reached, happens on a drawn day on or after the issue.
    /// </summary>
    public Drawing GenerateIn(DrawingStatus status) {
        Drawing drawing = WithDimensions(d => d.NonEmpty()).Generate();

        if (status >= DrawingStatus.Checked) { drawing.Check(); }
        if (status >= DrawingStatus.Approved) { drawing.Approve(); }
        if (status >= DrawingStatus.Released) {
            drawing.Release(Any.DateOnly().AfterOrEqualTo(drawing.IssuedOn).Generate());
        }

        return drawing;
    }

}
