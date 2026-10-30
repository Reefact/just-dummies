// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DocumentLink --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DocumentLink" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDocumentLink : IAny<DocumentLink> {

    private readonly IAny<DocumentKind> _kind;
    private readonly IAny<Uri>          _location;
    private readonly IAny<DateTime>     _uploadedAtUtc;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDocumentLink()
        : this(kind:          AnyValidKind(),
               location:      AnyValidLocation(),
               uploadedAtUtc: AnyValidUploadedAtUtc().As(AsUtc)) { }

    private static AnyEnum<DocumentKind> AnyValidKind() {
        return Any.Enum<DocumentKind>();
    }

    private static AnyWebUri AnyValidLocation() {
        // The document store only serves absolute https links.
        return Any.Uri().Web().UsingHttps();
    }

    private static AnyDateTime AnyValidUploadedAtUtc() {
        // Upload instants are recorded in UTC; AnyDateTime has no way to declare the Kind.
        return Any.DateTime();
    }

    private static DateTime AsUtc(DateTime instant) {
        return DateTime.SpecifyKind(instant, DateTimeKind.Utc);
    }

    private AnyDocumentLink(IAny<DocumentKind> kind,
                            IAny<Uri>          location,
                            IAny<DateTime>     uploadedAtUtc) {
        _kind          = kind;
        _location      = location;
        _uploadedAtUtc = uploadedAtUtc;
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyDocumentLink WithKind(DocumentKind value) {
        return new AnyDocumentLink(new FixedValue<DocumentKind>(value), _location, _uploadedAtUtc);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDocumentLink WithKind(Func<AnyEnum<DocumentKind>, AnyEnum<DocumentKind>> configure) {
        return new AnyDocumentLink(configure(AnyValidKind()), _location, _uploadedAtUtc);
    }

    /// <summary>Pins <c>location</c> to a fixed value.</summary>
    public AnyDocumentLink WithLocation(Uri value) {
        return new AnyDocumentLink(_kind, new FixedValue<Uri>(value), _uploadedAtUtc);
    }

    /// <summary>Draws <c>location</c> from <c>AnyValidLocation()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDocumentLink WithLocation(Func<AnyWebUri, AnyWebUri> configure) {
        return new AnyDocumentLink(_kind, configure(AnyValidLocation()), _uploadedAtUtc);
    }

    /// <summary>Pins <c>uploadedAtUtc</c> to a fixed value.</summary>
    public AnyDocumentLink WithUploadedAtUtc(DateTime value) {
        return new AnyDocumentLink(_kind, _location, new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>uploadedAtUtc</c> from <c>AnyValidUploadedAtUtc()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDocumentLink WithUploadedAtUtc(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyDocumentLink(_kind, _location, configure(AnyValidUploadedAtUtc()).As(AsUtc));
    }

    /// <summary>Produces one arbitrary <see cref="DocumentLink" />.</summary>
    public DocumentLink Generate() {
        return new DocumentLink(_kind.Generate(),
                                _location.Generate(),
                                _uploadedAtUtc.Generate());
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
