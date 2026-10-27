// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Property --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using RealEstate.Tests;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Property" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyProperty : IAny<Property> {

    private readonly IAny<CadastralReference>          _parcel;
    private readonly IAny<PropertyKind>                _kind;
    private readonly IAny<Surface>                     _livingArea;
    private readonly IAny<EnergyPerformanceDiagnosis>  _diagnosis;
    private readonly IAny<IReadOnlyList<DocumentLink>> _documents;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyProperty()
        : this(parcel:     AnyValidParcel(),
               kind:       AnyValidKind(),
               livingArea: AnyValidLivingArea(),
               diagnosis:  AnyValidDiagnosis(),
               documents:  AnyValidDocuments()) { }

    private static AnyCadastralReference AnyValidParcel() {
        return new AnyCadastralReference();
    }

    private static AnyEnum<PropertyKind> AnyValidKind() {
        return Any.Enum<PropertyKind>();
    }

    private static AnySurface AnyValidLivingArea() {
        return new AnySurface();
    }

    private static AnyEnergyPerformanceDiagnosis AnyValidDiagnosis() {
        return new AnyEnergyPerformanceDiagnosis();
    }

    private static AnyList<DocumentLink> AnyValidDocuments() {
        // At most 20 documents, each attached once (by location).
        return Any.ListOf(new AnyDocumentLink()).WithMaxCount(Property.MaxDocuments).Distinct(DocumentLocationComparer.Instance);
    }

    private AnyProperty(IAny<CadastralReference>          parcel,
                        IAny<PropertyKind>                kind,
                        IAny<Surface>                     livingArea,
                        IAny<EnergyPerformanceDiagnosis>  diagnosis,
                        IAny<IReadOnlyList<DocumentLink>> documents) {
        _parcel     = parcel;
        _kind       = kind;
        _livingArea = livingArea;
        _diagnosis  = diagnosis;
        _documents  = documents;
    }

    /// <summary>Pins <c>parcel</c> to a fixed value.</summary>
    public AnyProperty WithParcel(CadastralReference value) {
        return new AnyProperty(new FixedValue<CadastralReference>(value), _kind, _livingArea, _diagnosis, _documents);
    }

    /// <summary>Draws <c>parcel</c> from <c>AnyValidParcel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyProperty WithParcel(Func<AnyCadastralReference, AnyCadastralReference> configure) {
        return new AnyProperty(configure(AnyValidParcel()), _kind, _livingArea, _diagnosis, _documents);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyProperty WithKind(PropertyKind value) {
        return new AnyProperty(_parcel, new FixedValue<PropertyKind>(value), _livingArea, _diagnosis, _documents);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyProperty WithKind(Func<AnyEnum<PropertyKind>, AnyEnum<PropertyKind>> configure) {
        return new AnyProperty(_parcel, configure(AnyValidKind()), _livingArea, _diagnosis, _documents);
    }

    /// <summary>Pins <c>livingArea</c> to a fixed value.</summary>
    public AnyProperty WithLivingArea(Surface value) {
        return new AnyProperty(_parcel, _kind, new FixedValue<Surface>(value), _diagnosis, _documents);
    }

    /// <summary>Draws <c>livingArea</c> from <c>AnyValidLivingArea()</c>, refined by <paramref name="configure" />.</summary>
    public AnyProperty WithLivingArea(Func<AnySurface, AnySurface> configure) {
        return new AnyProperty(_parcel, _kind, configure(AnyValidLivingArea()), _diagnosis, _documents);
    }

    /// <summary>Pins <c>diagnosis</c> to a fixed value.</summary>
    public AnyProperty WithDiagnosis(EnergyPerformanceDiagnosis value) {
        return new AnyProperty(_parcel, _kind, _livingArea, new FixedValue<EnergyPerformanceDiagnosis>(value), _documents);
    }

    /// <summary>Draws <c>diagnosis</c> from <c>AnyValidDiagnosis()</c>, refined by <paramref name="configure" />.</summary>
    public AnyProperty WithDiagnosis(Func<AnyEnergyPerformanceDiagnosis, AnyEnergyPerformanceDiagnosis> configure) {
        return new AnyProperty(_parcel, _kind, _livingArea, configure(AnyValidDiagnosis()), _documents);
    }

    /// <summary>Pins <c>documents</c> to a fixed value.</summary>
    public AnyProperty WithDocuments(IReadOnlyList<DocumentLink> value) {
        return new AnyProperty(_parcel, _kind, _livingArea, _diagnosis, new FixedValue<IReadOnlyList<DocumentLink>>(value));
    }

    /// <summary>Draws <c>documents</c> from <c>AnyValidDocuments()</c>, refined by <paramref name="configure" />.</summary>
    public AnyProperty WithDocuments(Func<AnyList<DocumentLink>, AnyList<DocumentLink>> configure) {
        return new AnyProperty(_parcel, _kind, _livingArea, _diagnosis, configure(AnyValidDocuments()));
    }

    /// <summary>Produces one arbitrary <see cref="Property" />.</summary>
    public Property Generate() {
        return new Property(_parcel.Generate(),
                            _kind.Generate(),
                            _livingArea.Generate(),
                            _diagnosis.Generate(),
                            _documents.Generate());
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
