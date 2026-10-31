using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

namespace PulmonaryNodules.Tests.Expert;

/// <summary>
///     Recipes for the scenarios a surveillance test sets up: exams that can be registered onto one
///     another, a patient who lies differently on the table at each acquisition, and the nodule seen again.
/// </summary>
internal static class Thorax {

    /// <summary>
    ///     A position inside a patient's thorax. The scanner frame reaches ±2000 mm, but a body sits in the
    ///     middle of it, so a patient shift between acquisitions keeps every position inside the frame.
    /// </summary>
    public static readonly Func<AnySpatialPoint, AnySpatialPoint> Inside =
        p => p.WithXMm(x => x.Between(-250.0, 250.0))
              .WithYMm(y => y.Between(-250.0, 250.0))
              .WithZMm(z => z.Between(-400.0, 400.0));

    /// <summary>Anatomical landmarks located in one exam, each kind once, enough of them to register two exams.</summary>
    public static IAny<IReadOnlyCollection<AnatomicalLandmark>> Fiducials() {
        // Distinct kinds by construction: a set of kinds, each given a position.
        return Any.Combine(
            Any.SetOf(Any.Enum<LandmarkKind>()).WithMinCount(TranslationTransform.MinimumCommonLandmarks),
            Any.ListOf(Inside(Any.SpatialPoint())).WithCount(Enum.GetValues<LandmarkKind>().Length),
            (kinds, positions) => (IReadOnlyCollection<AnatomicalLandmark>)kinds.Zip(positions, (kind, position) => new AnatomicalLandmark(kind, position)).ToList());
    }

    /// <summary>An exam whose landmarks allow a later exam to be registered onto it.</summary>
    public static AnyCtExam RegistrableExam() {
        return Any.CtExam().WithLandmarks(Fiducials());
    }

    /// <summary>How differently a patient lies on the table from one acquisition to the next: a rigid shift.</summary>
    public static IAny<TranslationTransform> PatientRepositioning() {
        return Any.TranslationTransform()
                  .WithDxMm(d => d.Between(-50.0, 50.0))
                  .WithDyMm(d => d.Between(-50.0, 50.0))
                  .WithDzMm(d => d.Between(-80.0, 80.0))
                  .WithResidualErrorMm(0.0);
    }

    /// <summary>Any delay between two exams of a surveillance protocol.</summary>
    public static IAny<int> FollowUpDelayInDays() {
        return Any.Int32().Between(30, 730);
    }

    /// <summary>
    ///     A later acquisition of the same patient: another accession, its own acquisition geometry, and the
    ///     same anatomy seen through <paramref name="repositioning" />.
    /// </summary>
    public static AnyCtExam FollowUpOf(CtExam previous, TranslationTransform repositioning, int daysLater) {
        List<AnatomicalLandmark> landmarks = previous.Landmarks
                                                     .Select(l => new AnatomicalLandmark(l.Kind, repositioning.Apply(l.Position)))
                                                     .ToList();

        return Any.CtExam()
                  .WithAccessionNumber(a => a.DifferentFrom(previous.AccessionNumber))
                  .WithAcquiredOn(previous.AcquiredOn.AddDays(daysLater))
                  .WithLandmarks(landmarks);
    }

    /// <summary>The nodule segmented on <paramref name="exam" />, its centroid where the test puts it.</summary>
    public static NoduleObservation SegmentedAt(CtExam exam, SpatialPoint centroid) {
        return Any.NoduleObservation()
                  .WithExam(exam)
                  .WithSegmentation(s => s.WithCentroid(centroid))
                  .Generate();
    }

    /// <summary>A nodule under surveillance from a baseline exam that later exams can be registered onto.</summary>
    public static AnyTrackedNodule NoduleOnARegistrableBaseline() {
        return Any.TrackedNodule()
                  .WithBaseline(b => b.WithExam(_ => RegistrableExam())
                                      .WithSegmentation(s => s.WithCentroid(Inside)));
    }

    public static SpatialPoint Offset(this SpatialPoint point, double dxMm, double dyMm, double dzMm) {
        return new SpatialPoint(point.XMm + dxMm, point.YMm + dyMm, point.ZMm + dzMm);
    }

}
