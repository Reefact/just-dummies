using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyLanguageTag {

    /// <summary>A language alone, as an <see cref="ArticleRule" /> requires: no script subtag, no region.</summary>
    public AnyLanguageTag Bare() {
        AnyLanguageTag copy = new AnyLanguageTag(this);

        copy._value = Any.StringMatching("[A-Za-z]{2,3}");

        return copy;
    }

    /// <summary>A language with an optional region and no script subtag: it fits a record of any script.</summary>
    public AnyLanguageTag WithoutScript() {
        AnyLanguageTag copy = new AnyLanguageTag(this);

        copy._value = Any.StringMatching("[A-Za-z]{2,3}(-([A-Za-z]{2}|[0-9]{3}))?");

        return copy;
    }

}
