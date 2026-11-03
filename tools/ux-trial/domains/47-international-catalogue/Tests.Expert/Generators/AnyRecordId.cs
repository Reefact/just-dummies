using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hand-written: an agency code and a non-zero eight-digit number.</summary>
public sealed partial class AnyRecordId : IAny<RecordId> {

    public RecordId Generate() {
        return Any.Combine(Any.StringMatching("[A-Z]{2}"),
                           Any.Int32().Between(1, 99_999_999),
                           (agency, number) => new RecordId(agency + number.ToString("D8"))).Generate();
    }

}
