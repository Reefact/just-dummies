// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Witness --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Witness" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWitness : IAny<Witness> {

    private readonly IAny<WitnessSiglum> _siglum;
    private readonly IAny<string>        _repository;
    private readonly IAny<int>           _datedFrom;
    private readonly IAny<int>?          _datedTo;
    private readonly IAny<int>           _folioCount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWitness()
        : this(siglum:     AnyValidSiglum(),
               repository: AnyValidRepository(),
               datedFrom:  AnyValidDatedFrom(),
               datedTo:    null,
               folioCount: AnyValidFolioCount()) { }

    private static AnyWitnessSiglum AnyValidSiglum() {
        return new AnyWitnessSiglum();
    }

    private static AnyString AnyValidRepository() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    private static AnyInt32 AnyValidDatedFrom() {
        return Any.Int32().Between(Witness.EarliestYear, Witness.LatestYear);
    }

    private static AnyInt32 AnyValidDatedTo() {
        // Used only by a refinement: by default Generate() draws datedTo between the drawn datedFrom and LatestYear.
        return Any.Int32().Between(Witness.EarliestYear, Witness.LatestYear);
    }

    private static AnyInt32 AnyValidFolioCount() {
        return Any.Int32().Between(1, 1000);
    }

    private AnyWitness(IAny<WitnessSiglum> siglum,
                       IAny<string>        repository,
                       IAny<int>           datedFrom,
                       IAny<int>?          datedTo,
                       IAny<int>           folioCount) {
        _siglum     = siglum;
        _repository = repository;
        _datedFrom  = datedFrom;
        _datedTo    = datedTo;
        _folioCount = folioCount;
    }

    /// <summary>Pins <c>siglum</c> to a fixed value.</summary>
    public AnyWitness WithSiglum(WitnessSiglum value) {
        return new AnyWitness(new FixedValue<WitnessSiglum>(value), _repository, _datedFrom, _datedTo, _folioCount);
    }

    /// <summary>Draws <c>siglum</c> from <c>AnyValidSiglum()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitness WithSiglum(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        return new AnyWitness(configure(AnyValidSiglum()), _repository, _datedFrom, _datedTo, _folioCount);
    }

    /// <summary>Pins <c>repository</c> to a fixed value.</summary>
    public AnyWitness WithRepository(string value) {
        return new AnyWitness(_siglum, new FixedValue<string>(value), _datedFrom, _datedTo, _folioCount);
    }

    /// <summary>Draws <c>repository</c> from <c>AnyValidRepository()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitness WithRepository(Func<AnyString, AnyString> configure) {
        return new AnyWitness(_siglum, configure(AnyValidRepository()), _datedFrom, _datedTo, _folioCount);
    }

    /// <summary>Pins <c>datedFrom</c> to a fixed value.</summary>
    public AnyWitness WithDatedFrom(int value) {
        return new AnyWitness(_siglum, _repository, new FixedValue<int>(value), _datedTo, _folioCount);
    }

    /// <summary>Draws <c>datedFrom</c> from <c>AnyValidDatedFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitness WithDatedFrom(Func<AnyInt32, AnyInt32> configure) {
        return new AnyWitness(_siglum, _repository, configure(AnyValidDatedFrom()), _datedTo, _folioCount);
    }

    /// <summary>Pins <c>datedTo</c> to a fixed value.</summary>
    public AnyWitness WithDatedTo(int value) {
        return new AnyWitness(_siglum, _repository, _datedFrom, new FixedValue<int>(value), _folioCount);
    }

    /// <summary>Draws <c>datedTo</c> from <c>AnyValidDatedTo()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitness WithDatedTo(Func<AnyInt32, AnyInt32> configure) {
        return new AnyWitness(_siglum, _repository, _datedFrom, configure(AnyValidDatedTo()), _folioCount);
    }

    /// <summary>Pins <c>folioCount</c> to a fixed value.</summary>
    public AnyWitness WithFolioCount(int value) {
        return new AnyWitness(_siglum, _repository, _datedFrom, _datedTo, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>folioCount</c> from <c>AnyValidFolioCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitness WithFolioCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyWitness(_siglum, _repository, _datedFrom, _datedTo, configure(AnyValidFolioCount()));
    }

    /// <summary>Produces one arbitrary <see cref="Witness" />.</summary>
    public Witness Generate() {
        // A dating span ends no earlier than it starts: unless pinned or refined, datedTo is derived from datedFrom.
        int datedFrom = _datedFrom.Generate();
        int datedTo   = _datedTo?.Generate() ?? Any.Int32().Between(datedFrom, Witness.LatestYear).Generate();

        return new Witness(_siglum.Generate(),
                           _repository.Generate(),
                           datedFrom,
                           datedTo,
                           _folioCount.Generate());
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
