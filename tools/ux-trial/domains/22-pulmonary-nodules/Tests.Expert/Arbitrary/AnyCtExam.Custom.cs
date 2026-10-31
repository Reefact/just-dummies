using System.Collections.Generic;

using JustDummies;

using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

// Written by hand: `any generate CtExam --force` leaves this file alone.
public sealed partial class AnyCtExam {

    /// <summary>
    ///     Draws the landmarks from <paramref name="generator" />, whose elements are drawn another way than
    ///     the base. It replaces the base: the generator must keep each landmark kind at most once.
    /// </summary>
    public AnyCtExam WithLandmarks(IAny<IReadOnlyCollection<AnatomicalLandmark>> generator) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, _sliceThicknessMm, generator);
    }

}
