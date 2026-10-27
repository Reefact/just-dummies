// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Prescription --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Prescription" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrescription : IAny<Prescription> {

    private readonly IAny<Guid>                            _id;
    private readonly IAny<Patient>                         _patient;
    private readonly IAny<string>                          _prescriberRpps;
    private readonly IAny<DateOnly>                        _issuedOn;
    private readonly IAny<IReadOnlyList<PrescriptionLine>> _lines;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrescription()
        : this(id:             AnyValidId(),
               patient:        AnyValidPatient(),
               prescriberRpps: AnyValidPrescriberRpps(),
               issuedOn:       AnyValidIssuedOn(),
               lines:          AnyValidLines()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyPatient AnyValidPatient() {
        // Patients are born by 2025-12-31 and prescriptions are issued from 2026: issuedOn never predates the patient.
        return new AnyPatient();
    }

    private static AnyString AnyValidPrescriberRpps() {
        // An RPPS number is eleven ASCII digits.
        return Any.String().Numeric().WithLength(11);
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2030, 12, 31));
    }

    private static AnyList<PrescriptionLine> AnyValidLines() {
        return Any.ListOf(new AnyPrescriptionLine()).WithCountBetween(1, Prescription.MaxLines);
    }

    private AnyPrescription(IAny<Guid>                            id,
                            IAny<Patient>                         patient,
                            IAny<string>                          prescriberRpps,
                            IAny<DateOnly>                        issuedOn,
                            IAny<IReadOnlyList<PrescriptionLine>> lines) {
        _id             = id;
        _patient        = patient;
        _prescriberRpps = prescriberRpps;
        _issuedOn       = issuedOn;
        _lines          = lines;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyPrescription WithId(Guid value) {
        return new AnyPrescription(new FixedValue<Guid>(value), _patient, _prescriberRpps, _issuedOn, _lines);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescription WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyPrescription(configure(AnyValidId()), _patient, _prescriberRpps, _issuedOn, _lines);
    }

    /// <summary>Pins <c>patient</c> to a fixed value.</summary>
    public AnyPrescription WithPatient(Patient value) {
        return new AnyPrescription(_id, new FixedValue<Patient>(value), _prescriberRpps, _issuedOn, _lines);
    }

    /// <summary>Draws <c>patient</c> from <c>AnyValidPatient()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescription WithPatient(Func<AnyPatient, AnyPatient> configure) {
        return new AnyPrescription(_id, configure(AnyValidPatient()), _prescriberRpps, _issuedOn, _lines);
    }

    /// <summary>Pins <c>prescriberRpps</c> to a fixed value.</summary>
    public AnyPrescription WithPrescriberRpps(string value) {
        return new AnyPrescription(_id, _patient, new FixedValue<string>(value), _issuedOn, _lines);
    }

    /// <summary>Draws <c>prescriberRpps</c> from <c>AnyValidPrescriberRpps()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescription WithPrescriberRpps(Func<AnyString, AnyString> configure) {
        return new AnyPrescription(_id, _patient, configure(AnyValidPrescriberRpps()), _issuedOn, _lines);
    }

    /// <summary>Pins <c>issuedOn</c> to a fixed value.</summary>
    public AnyPrescription WithIssuedOn(DateOnly value) {
        return new AnyPrescription(_id, _patient, _prescriberRpps, new FixedValue<DateOnly>(value), _lines);
    }

    /// <summary>Draws <c>issuedOn</c> from <c>AnyValidIssuedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescription WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyPrescription(_id, _patient, _prescriberRpps, configure(AnyValidIssuedOn()), _lines);
    }

    /// <summary>Pins <c>lines</c> to a fixed value.</summary>
    public AnyPrescription WithLines(IReadOnlyList<PrescriptionLine> value) {
        return new AnyPrescription(_id, _patient, _prescriberRpps, _issuedOn, new FixedValue<IReadOnlyList<PrescriptionLine>>(value));
    }

    /// <summary>Draws <c>lines</c> from <c>AnyValidLines()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescription WithLines(Func<AnyList<PrescriptionLine>, AnyList<PrescriptionLine>> configure) {
        return new AnyPrescription(_id, _patient, _prescriberRpps, _issuedOn, configure(AnyValidLines()));
    }

    /// <summary>Produces one arbitrary <see cref="Prescription" />.</summary>
    public Prescription Generate() {
        return new Prescription(_id.Generate(),
                                _patient.Generate(),
                                _prescriberRpps.Generate(),
                                _issuedOn.Generate(),
                                _lines.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
