using CaptiveBreeding.Domain.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class PedigreeTests {

    private const int Precision = 12;

    /// <summary>
    ///     Two unrelated founders (<see cref="Sire" />, <see cref="Dam" />) with two full siblings
    ///     (<see cref="Son" />, <see cref="Daughter" />), their inbred child (<see cref="Inbred" />), a half
    ///     sister (<see cref="HalfSister" />, by the sire) and two first cousins (<see cref="CousinX" />,
    ///     <see cref="CousinY" />).
    /// </summary>
    private sealed class Family {

        public Family() {
            Builder    = new PedigreeBuilder();
            Sire       = Builder.Founder(Sex.Male);
            Dam        = Builder.Founder(Sex.Female);
            Son        = Builder.Child(Sire, Dam, Sex.Male);
            Daughter   = Builder.Child(Sire, Dam, Sex.Female);
            Inbred     = Builder.Child(Son, Daughter, Any.Enum<Sex>().Generate());
            Other      = Builder.Founder(Sex.Female);
            HalfSister = Builder.Child(Sire, Other, Sex.Female);
            Spouse     = Builder.Founder(Sex.Female);
            Husband    = Builder.Founder(Sex.Male);
            CousinX    = Builder.Child(Son, Spouse, Any.Enum<Sex>().Generate());
            CousinY    = Builder.Child(Husband, Daughter, Any.Enum<Sex>().Generate());
        }

        public PedigreeBuilder Builder { get; }

        public Pedigree Pedigree => Builder.Pedigree;

        public Individual Sire { get; }
        public Individual Dam { get; }
        public Individual Son { get; }
        public Individual Daughter { get; }
        public Individual Inbred { get; }
        public Individual Other { get; }
        public Individual HalfSister { get; }
        public Individual Spouse { get; }
        public Individual Husband { get; }
        public Individual CousinX { get; }
        public Individual CousinY { get; }

        public double Kinship(Individual first, Individual second) {
            return Pedigree.KinshipBetween(first.Id, second.Id).Value;
        }

    }

    // --- Construction ------------------------------------------------------

    [Fact]
    public void A_pedigree_keeps_its_programme_and_its_species() {
        // Arrange
        string anyProgramme = Any.String().NotBlank().Generate();
        string anySpecies   = Any.String().NotBlank().Generate();

        // Act
        Pedigree pedigree = new Pedigree(anyProgramme, anySpecies);

        // Assert
        Assert.Equal(anyProgramme, pedigree.ProgrammeName);
        Assert.Equal(anySpecies, pedigree.Species);
        Assert.Empty(pedigree.Individuals);
    }

    [Fact]
    public void A_pedigree_refuses_a_missing_programme_or_species() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Pedigree(null!, anyName));
        Assert.Throws<ArgumentNullException>(() => new Pedigree(anyName, null!));
    }

    [Fact]
    public void A_pedigree_refuses_a_blank_programme() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().WithLengthBetween(0, 20).Generate();
        string anyName  = Any.String().NotBlank().Generate();

        // Act
        Action construction = () => new Pedigree(anyBlank, anyName);

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void A_pedigree_refuses_a_blank_species() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().WithLengthBetween(0, 20).Generate();
        string anyName  = Any.String().NotBlank().Generate();

        // Act
        Action construction = () => new Pedigree(anyName, anyBlank);

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    // --- Register, Get, Contains -------------------------------------------

    [Fact]
    public void A_registered_individual_can_be_found_by_its_id() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();

        // Act
        Individual founder = builder.Founder(Sex.Male);

        // Assert
        Assert.True(builder.Pedigree.Contains(founder.Id));
        Assert.Same(founder, builder.Pedigree.Get(founder.Id));
        Assert.Single(builder.Pedigree.Individuals);
    }

    [Fact]
    public void An_unregistered_id_is_unknown() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        builder.Founder(Sex.Male);

        // Act / Assert
        Assert.False(builder.Pedigree.Contains(builder.NextId()));
    }

    [Fact]
    public void Getting_an_unknown_individual_names_it_and_the_programme() {
        // Arrange
        string          anyProgramme = Any.String().Alpha().WithLengthBetween(3, 30).Generate();
        Pedigree        pedigree     = Any.Pedigree().WithProgrammeName(anyProgramme).Generate();
        StudbookId      anyUnknown   = Any.StudbookId().Generate();

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => pedigree.Get(anyUnknown));

        // Assert
        Assert.Contains(anyUnknown.Value, refusal.Message);
        Assert.Contains(anyProgramme, refusal.Message);
    }

    [Fact]
    public void Getting_a_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Pedigree().Generate().Get(null!));
    }

    [Fact]
    public void Registering_a_missing_individual_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Pedigree().Generate().Register(null!));
    }

    [Fact]
    public void Registering_the_same_id_twice_is_refused() {
        // Arrange
        PedigreeBuilder builder  = new PedigreeBuilder();
        Individual      founder  = builder.Founder(Sex.Male);
        Individual      namesake = Any.Individual().WithId(founder.Id).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(namesake);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
        Assert.Single(builder.Pedigree.Individuals);
        Assert.Same(founder, builder.Pedigree.Get(founder.Id));
    }

    [Fact]
    public void Registering_the_very_same_individual_twice_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      founder = builder.Founder(Sex.Female);

        // Act
        Action registration = () => builder.Pedigree.Register(founder);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
    }

    [Fact]
    public void A_child_of_an_unregistered_sire_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      dam     = builder.Founder(Sex.Female);
        Individual      orphan  = Any.Individual().WithId(builder.NextId()).ChildOf(builder.NextId(), dam.Id).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(orphan);

        // Assert
        Assert.Throws<KeyNotFoundException>(registration);
        Assert.False(builder.Pedigree.Contains(orphan.Id));
    }

    [Fact]
    public void A_child_of_an_unregistered_dam_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male);
        Individual      orphan  = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, builder.NextId()).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(orphan);

        // Assert
        Assert.Throws<KeyNotFoundException>(registration);
        Assert.False(builder.Pedigree.Contains(orphan.Id));
    }

    [Fact]
    public void A_sire_that_is_not_male_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      notMale = builder.Founder(Sex.Female);
        Individual      dam     = builder.Founder(Sex.Female);
        Individual      child   = Any.Individual().WithId(builder.NextId()).ChildOf(notMale.Id, dam.Id).WithBirthDate(new DateOnly(2030, 1, 1)).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
        Assert.False(builder.Pedigree.Contains(child.Id));
    }

    [Fact]
    public void A_dam_that_is_not_female_is_refused() {
        // Arrange
        PedigreeBuilder builder   = new PedigreeBuilder();
        Individual      sire      = builder.Founder(Sex.Male);
        Individual      notFemale = builder.Founder(Sex.Male);
        Individual      child     = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, notFemale.Id).WithBirthDate(new DateOnly(2030, 1, 1)).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
    }

    [Fact]
    public void A_child_born_the_same_day_as_its_sire_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male);
        Individual      dam     = builder.Founder(Sex.Female, sire.BirthDate.AddYears(-1));
        Individual      child   = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, dam.Id).WithBirthDate(sire.BirthDate).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
    }

    [Fact]
    public void A_child_born_the_same_day_as_its_dam_is_refused() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      dam     = builder.Founder(Sex.Female);
        Individual      sire    = builder.Founder(Sex.Male, dam.BirthDate.AddYears(-1));
        Individual      child   = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, dam.Id).WithBirthDate(dam.BirthDate).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
    }

    [Fact]
    public void A_child_born_before_one_of_its_parents_is_refused() {
        // Arrange
        PedigreeBuilder builder  = new PedigreeBuilder();
        Individual      sire     = builder.Founder(Sex.Male);
        Individual      dam      = builder.Founder(Sex.Female);
        DateOnly        anyEarly = Any.DateOnly().Before(sire.BirthDate).Generate();
        Individual      child    = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, dam.Id).WithBirthDate(anyEarly).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
    }

    [Fact]
    public void A_child_born_one_day_after_both_parents_is_accepted() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male, new DateOnly(2001, 5, 10));
        Individual      dam     = builder.Founder(Sex.Female, new DateOnly(2002, 7, 20));

        // Act
        Individual child = builder.Child(sire, dam, Any.Enum<Sex>().Generate(), new DateOnly(2002, 7, 21));

        // Assert
        Assert.True(builder.Pedigree.Contains(child.Id));
    }

    [Fact]
    public void A_dam_that_died_before_the_birth_is_refused() {
        // Arrange
        PedigreeBuilder builder  = new PedigreeBuilder();
        Individual      sire     = builder.Founder(Sex.Male);
        Individual      dam      = builder.Founder(Sex.Female);
        DateOnly        birth    = (sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate).AddYears(3);
        DateOnly        anyDeath = Any.DateOnly().Between(dam.BirthDate, birth.AddDays(-1)).Generate();
        dam.RecordDeath(anyDeath);
        Individual child = Any.Individual().WithId(builder.NextId()).ChildOf(sire.Id, dam.Id).WithBirthDate(birth).Generate();

        // Act
        Action registration = () => builder.Pedigree.Register(child);

        // Assert
        Assert.Throws<InvalidOperationException>(registration);
        Assert.False(builder.Pedigree.Contains(child.Id));
    }

    [Fact]
    public void A_dam_that_died_on_the_day_of_the_birth_is_accepted() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male);
        Individual      dam     = builder.Founder(Sex.Female);
        DateOnly        birth   = (sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate).AddYears(3);
        dam.RecordDeath(birth);

        // Act
        Individual child = builder.Child(sire, dam, Any.Enum<Sex>().Generate(), birth);

        // Assert
        Assert.True(builder.Pedigree.Contains(child.Id));
    }

    [Fact]
    public void A_dam_that_died_after_the_birth_is_accepted() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male);
        Individual      dam     = builder.Founder(Sex.Female);
        DateOnly        birth   = (sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate).AddYears(3);
        dam.RecordDeath(birth.AddDays(Any.Int32().Between(1, 3_000).Generate()));

        // Act
        Individual child = builder.Child(sire, dam, Any.Enum<Sex>().Generate(), birth);

        // Assert
        Assert.True(builder.Pedigree.Contains(child.Id));
    }

    [Fact]
    public void Individuals_are_listed_in_the_order_they_were_registered() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlyList<Individual> individuals = family.Pedigree.Individuals;

        // Assert
        Assert.Equal(11, individuals.Count);
        Assert.Same(family.Sire, individuals[0]);
        Assert.Same(family.Dam, individuals[1]);
        Assert.Same(family.Son, individuals[2]);
        Assert.Same(family.CousinY, individuals[10]);
    }

    [Fact]
    public void Only_the_founders_are_founders_and_only_the_living_are_living() {
        // Arrange
        Family family = new Family();
        family.Son.RecordDeath(family.Son.BirthDate.AddYears(1));

        // Act
        IReadOnlyList<Individual> founders = family.Pedigree.Founders.ToList();
        IReadOnlyList<Individual> living   = family.Pedigree.Living.ToList();

        // Assert
        Assert.Equal(5, founders.Count);
        Assert.All(founders, f => Assert.True(f.IsFounder));
        Assert.Equal(10, living.Count);
        Assert.DoesNotContain(family.Son, living);
    }

    // --- Generations and ancestry -----------------------------------------

    [Fact]
    public void A_founder_is_generation_zero() {
        Family family = new Family();

        Assert.Equal(0, family.Pedigree.GenerationOf(family.Sire.Id));
    }

    [Fact]
    public void The_child_of_two_founders_is_generation_one() {
        Family family = new Family();

        Assert.Equal(1, family.Pedigree.GenerationOf(family.Son.Id));
    }

    [Fact]
    public void A_child_of_siblings_is_generation_two() {
        Family family = new Family();

        Assert.Equal(2, family.Pedigree.GenerationOf(family.Inbred.Id));
    }

    [Fact]
    public void The_generation_follows_the_deepest_parent() {
        // Arrange
        Family family = new Family();

        // Act
        int generation = family.Pedigree.GenerationOf(family.CousinX.Id);

        // Assert
        Assert.Equal(2, generation);
    }

    [Fact]
    public void The_generation_of_an_unknown_individual_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => new Family().Pedigree.GenerationOf(Any.StudbookId().Generate()));
    }

    [Fact]
    public void A_founder_has_no_ancestors() {
        Family family = new Family();

        Assert.Empty(family.Pedigree.AncestorsOf(family.Sire.Id));
    }

    [Fact]
    public void The_ancestors_are_all_the_individuals_up_the_two_lines() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> ancestors = family.Pedigree.AncestorsOf(family.CousinX.Id);

        // Assert
        Assert.Equal(4, ancestors.Count);
        Assert.Contains(family.Son.Id, ancestors);
        Assert.Contains(family.Spouse.Id, ancestors);
        Assert.Contains(family.Sire.Id, ancestors);
        Assert.Contains(family.Dam.Id, ancestors);
    }

    [Fact]
    public void An_individual_is_not_its_own_ancestor() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> ancestors = family.Pedigree.AncestorsOf(family.Inbred.Id);

        // Assert
        Assert.DoesNotContain(family.Inbred.Id, ancestors);
        Assert.Equal(4, ancestors.Count);
    }

    [Fact]
    public void The_ancestors_of_an_unknown_individual_are_refused() {
        Assert.Throws<KeyNotFoundException>(() => new Family().Pedigree.AncestorsOf(Any.StudbookId().Generate()));
    }

    [Fact]
    public void Full_siblings_share_both_parents_as_common_ancestors() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> common = family.Pedigree.CommonAncestorsOf(family.Son.Id, family.Daughter.Id);

        // Assert
        Assert.Equal(2, common.Count);
        Assert.Contains(family.Sire.Id, common);
        Assert.Contains(family.Dam.Id, common);
    }

    [Fact]
    public void Half_siblings_share_only_the_common_parent() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> common = family.Pedigree.CommonAncestorsOf(family.Daughter.Id, family.HalfSister.Id);

        // Assert
        Assert.Equal(new[] { family.Sire.Id }, common);
    }

    [Fact]
    public void First_cousins_share_their_grandparents() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> common = family.Pedigree.CommonAncestorsOf(family.CousinX.Id, family.CousinY.Id);

        // Assert
        Assert.Equal(2, common.Count);
        Assert.Contains(family.Sire.Id, common);
        Assert.Contains(family.Dam.Id, common);
    }

    [Fact]
    public void Unrelated_founders_have_no_common_ancestor() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlySet<StudbookId> common = family.Pedigree.CommonAncestorsOf(family.Sire.Id, family.Dam.Id);

        // Assert
        Assert.Empty(common);
    }

    [Fact]
    public void Common_ancestors_leave_the_ancestors_of_each_untouched() {
        // Arrange
        Family family = new Family();

        // Act
        family.Pedigree.CommonAncestorsOf(family.Son.Id, family.Daughter.Id);

        // Assert
        Assert.Equal(2, family.Pedigree.AncestorsOf(family.Son.Id).Count);
    }

    // --- Kinship -----------------------------------------------------------

    [Fact]
    public void Unrelated_founders_have_no_kinship() {
        Family family = new Family();

        Assert.Equal(0d, family.Kinship(family.Sire, family.Dam));
    }

    [Fact]
    public void A_founder_has_a_kinship_of_one_half_with_itself() {
        Family family = new Family();

        Assert.Equal(0.5d, family.Kinship(family.Sire, family.Sire));
    }

    [Fact]
    public void A_parent_and_its_offspring_have_a_kinship_of_a_quarter() {
        Family family = new Family();

        Assert.Equal(0.25d, family.Kinship(family.Sire, family.Son), Precision);
        Assert.Equal(0.25d, family.Kinship(family.Dam, family.Son), Precision);
    }

    [Fact]
    public void Full_siblings_have_a_kinship_of_a_quarter() {
        Family family = new Family();

        Assert.Equal(0.25d, family.Kinship(family.Son, family.Daughter), Precision);
    }

    [Fact]
    public void Half_siblings_have_a_kinship_of_an_eighth() {
        Family family = new Family();

        Assert.Equal(0.125d, family.Kinship(family.Daughter, family.HalfSister), Precision);
    }

    [Fact]
    public void First_cousins_have_a_kinship_of_a_sixteenth() {
        Family family = new Family();

        Assert.Equal(0.0625d, family.Kinship(family.CousinX, family.CousinY), Precision);
    }

    [Fact]
    public void An_inbred_individual_has_more_than_one_half_with_itself() {
        Family family = new Family();

        Assert.Equal(0.625d, family.Kinship(family.Inbred, family.Inbred), Precision);
    }

    [Fact]
    public void An_inbred_individual_is_closer_to_its_parents_than_a_quarter() {
        Family family = new Family();

        Assert.Equal(0.375d, family.Kinship(family.Inbred, family.Son), Precision);
        Assert.Equal(0.375d, family.Kinship(family.Inbred, family.Daughter), Precision);
    }

    [Fact]
    public void Kinship_does_not_depend_on_the_order_of_the_arguments() {
        // Arrange
        Family family = new Family();

        // Act
        double forward  = family.Kinship(family.CousinX, family.Inbred);
        double backward = family.Kinship(family.Inbred, family.CousinX);

        // Assert
        Assert.Equal(forward, backward, Precision);
    }

    [Fact]
    public void Kinship_is_stable_when_asked_again_after_the_pedigree_grew() {
        // Arrange
        Family family = new Family();
        double before = family.Kinship(family.Son, family.Daughter);

        // Act
        family.Builder.Child(family.Son, family.Daughter, Any.Enum<Sex>().Generate());
        double after = family.Kinship(family.Son, family.Daughter);

        // Assert
        Assert.Equal(before, after, Precision);
    }

    [Fact]
    public void Kinship_with_an_unknown_individual_is_refused_on_either_side() {
        // Arrange
        Family     family     = new Family();
        StudbookId anyUnknown = Any.StudbookId().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => family.Pedigree.KinshipBetween(anyUnknown, family.Sire.Id));
        Assert.Throws<KeyNotFoundException>(() => family.Pedigree.KinshipBetween(family.Sire.Id, anyUnknown));
    }

    [Fact]
    public void A_founder_is_not_inbred() {
        Family family = new Family();

        Assert.Equal(0d, family.Pedigree.InbreedingOf(family.Sire.Id));
    }

    [Fact]
    public void The_child_of_unrelated_parents_is_not_inbred() {
        Family family = new Family();

        Assert.Equal(0d, family.Pedigree.InbreedingOf(family.Son.Id));
    }

    [Fact]
    public void The_child_of_full_siblings_is_inbred_by_a_quarter() {
        Family family = new Family();

        Assert.Equal(0.25d, family.Pedigree.InbreedingOf(family.Inbred.Id), Precision);
    }

    [Fact]
    public void The_inbreeding_of_an_individual_is_the_kinship_of_its_parents() {
        // Arrange
        Family family = new Family();

        // Act
        double inbreeding = family.Pedigree.InbreedingOf(family.Inbred.Id);

        // Assert
        Assert.Equal(family.Kinship(family.Son, family.Daughter), inbreeding, Precision);
    }

    [Fact]
    public void The_inbreeding_of_an_unknown_individual_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => new Family().Pedigree.InbreedingOf(Any.StudbookId().Generate()));
    }

    // --- Mean kinship ------------------------------------------------------

    private static (PedigreeBuilder Builder, Individual Sire, Individual Dam, Individual Child) Trio() {
        PedigreeBuilder builder = new PedigreeBuilder();
        Individual      sire    = builder.Founder(Sex.Male);
        Individual      dam     = builder.Founder(Sex.Female);
        Individual      child   = builder.Child(sire, dam, Any.Enum<Sex>().Generate());

        return (builder, sire, dam, child);
    }

    [Fact]
    public void The_mean_kinship_of_a_parent_averages_over_every_living_individual_itself_included() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, _, _) = Trio();

        // Act
        double mean = builder.Pedigree.MeanKinshipOf(sire.Id);

        // Assert
        Assert.Equal((0.5d + 0d + 0.25d) / 3d, mean, Precision);
    }

    [Fact]
    public void The_mean_kinship_of_the_child_is_higher_than_that_of_a_parent() {
        // Arrange
        (PedigreeBuilder builder, _, _, Individual child) = Trio();

        // Act
        double mean = builder.Pedigree.MeanKinshipOf(child.Id);

        // Assert
        Assert.Equal((0.25d + 0.25d + 0.5d) / 3d, mean, Precision);
    }

    [Fact]
    public void The_dead_are_left_out_of_the_mean_kinship() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, _) = Trio();
        dam.RecordDeath(dam.BirthDate.AddYears(1));

        // Act
        double mean = builder.Pedigree.MeanKinshipOf(sire.Id);

        // Assert
        Assert.Equal((0.5d + 0.25d) / 2d, mean, Precision);
    }

    [Fact]
    public void A_dead_individual_still_has_a_mean_kinship_with_the_living() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, _, _) = Trio();
        sire.RecordDeath(sire.BirthDate.AddYears(1));

        // Act
        double mean = builder.Pedigree.MeanKinshipOf(sire.Id);

        // Assert
        Assert.Equal((0d + 0.25d) / 2d, mean, Precision);
    }

    [Fact]
    public void The_mean_kinship_is_undefined_when_nobody_is_alive() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, Individual child) = Trio();
        foreach (Individual individual in new[] { sire, dam, child }) {
            individual.RecordDeath(child.BirthDate.AddYears(Any.Int32().Between(0, 20).Generate()));
        }

        // Act
        Action calculation = () => builder.Pedigree.MeanKinshipOf(sire.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(calculation);
    }

    [Fact]
    public void The_mean_kinship_is_undefined_for_an_empty_pedigree() {
        // Arrange
        Pedigree empty = Any.Pedigree().Generate();

        // Act
        Action calculation = () => empty.MeanKinshipOf(Any.StudbookId().Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(calculation);
    }

    [Fact]
    public void The_mean_kinship_of_an_unknown_individual_is_refused() {
        // Arrange
        (PedigreeBuilder builder, _, _, _) = Trio();

        // Act
        Action calculation = () => builder.Pedigree.MeanKinshipOf(builder.NextId());

        // Assert
        Assert.Throws<KeyNotFoundException>(calculation);
    }

    // --- Founder contributions --------------------------------------------

    private static double ShareOf(IReadOnlyList<GeneticContribution> contributions, Individual founder) {
        return contributions.Single(c => c.FounderId == founder.Id).Proportion;
    }

    [Fact]
    public void Each_founder_of_a_trio_holds_half_of_the_living_genes() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, _) = Trio();

        // Act
        IReadOnlyList<GeneticContribution> contributions = builder.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(2, contributions.Count);
        Assert.Equal(0.5d, ShareOf(contributions, sire), Precision);
        Assert.Equal(0.5d, ShareOf(contributions, dam), Precision);
    }

    [Fact]
    public void The_contributions_of_the_founders_add_up_to_one() {
        // Arrange
        Family family = new Family();

        // Act
        IReadOnlyList<GeneticContribution> contributions = family.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(1d, contributions.Sum(c => c.Proportion), Precision);
        Assert.Equal(family.Pedigree.Founders.Count(), contributions.Count);
    }

    [Fact]
    public void A_founder_without_living_descendants_is_lost() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, _) = Trio();
        Individual lonely = builder.Founder(Sex.Female);
        lonely.RecordDeath(lonely.BirthDate.AddYears(2));

        // Act
        IReadOnlyList<GeneticContribution> contributions = builder.Pedigree.FounderContributions();

        // Assert
        GeneticContribution lost = contributions.Single(c => c.FounderId == lonely.Id);
        Assert.True(lost.IsLost);
        Assert.Equal(0.5d, ShareOf(contributions, sire), Precision);
        Assert.Equal(0.5d, ShareOf(contributions, dam), Precision);
    }

    [Fact]
    public void A_dead_founder_whose_descendants_live_still_contributes() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, Individual child) = Trio();
        sire.RecordDeath(sire.BirthDate.AddYears(1));
        dam.RecordDeath(dam.BirthDate.AddYears(1));

        // Act
        IReadOnlyList<GeneticContribution> contributions = builder.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(0.5d, ShareOf(contributions, sire), Precision);
        Assert.Equal(0.5d, ShareOf(contributions, dam), Precision);
        Assert.True(child.IsAlive);
    }

    [Fact]
    public void An_unequal_ancestry_gives_unequal_contributions() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, _) = Trio();
        dam.RecordDeath(dam.BirthDate.AddYears(1));

        // Act
        IReadOnlyList<GeneticContribution> contributions = builder.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(0.75d, ShareOf(contributions, sire), Precision);
        Assert.Equal(0.25d, ShareOf(contributions, dam), Precision);
    }

    [Fact]
    public void The_contributions_are_undefined_when_nobody_is_alive() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, Individual child) = Trio();
        foreach (Individual individual in new[] { sire, dam, child }) {
            individual.RecordDeath(child.BirthDate.AddYears(1));
        }

        // Act
        Action calculation = () => builder.Pedigree.FounderContributions();

        // Assert
        Assert.Throws<InvalidOperationException>(calculation);
    }

    [Fact]
    public void The_contributions_are_undefined_for_an_empty_pedigree() {
        Assert.Throws<InvalidOperationException>(() => Any.Pedigree().Generate().FounderContributions());
    }

    // --- Founder equivalents ----------------------------------------------

    [Fact]
    public void Unrelated_founders_alone_are_worth_as_many_equivalents_as_they_are() {
        // Arrange
        PedigreeBuilder builder = new PedigreeBuilder();
        int             anyCount = Any.Int32().Between(1, 8).Generate();
        for (int i = 0; i < anyCount; i++) {
            builder.Founder(Any.Enum<Sex>().Generate());
        }

        // Act
        double equivalents = builder.Pedigree.FounderEquivalents();

        // Assert
        Assert.Equal(anyCount, equivalents, Precision);
    }

    [Fact]
    public void Two_founders_with_equal_shares_are_worth_two_equivalents() {
        // Arrange
        (PedigreeBuilder builder, _, _, _) = Trio();

        // Act
        double equivalents = builder.Pedigree.FounderEquivalents();

        // Assert
        Assert.Equal(2d, equivalents, Precision);
    }

    [Fact]
    public void Unequal_shares_are_worth_fewer_equivalents_than_there_are_founders() {
        // Arrange
        (PedigreeBuilder builder, _, Individual dam, _) = Trio();
        dam.RecordDeath(dam.BirthDate.AddYears(1));

        // Act
        double equivalents = builder.Pedigree.FounderEquivalents();

        // Assert
        Assert.Equal(1d / (0.75d * 0.75d + 0.25d * 0.25d), equivalents, Precision);
    }

    [Fact]
    public void The_equivalents_are_undefined_when_nobody_is_alive() {
        // Arrange
        (PedigreeBuilder builder, Individual sire, Individual dam, Individual child) = Trio();
        foreach (Individual individual in new[] { sire, dam, child }) {
            individual.RecordDeath(child.BirthDate.AddYears(1));
        }

        // Act
        Action calculation = () => builder.Pedigree.FounderEquivalents();

        // Assert
        Assert.Throws<InvalidOperationException>(calculation);
    }

}
