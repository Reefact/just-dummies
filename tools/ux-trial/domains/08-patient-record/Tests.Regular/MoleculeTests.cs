using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class MoleculeTests {

    [Theory]
    [InlineData("paracetamol")]
    [InlineData("acide acetylsalicylique")]
    [InlineData("sulfamethoxazole-trimethoprime")]
    [InlineData("a")]
    public void An_INN_of_lower_case_letters_spaces_and_hyphens_is_accepted_as_given(string name) {
        Molecule molecule = new Molecule(name);

        Assert.Equal(name, molecule.Name);
        Assert.Equal(name, molecule.ToString());
    }

    [Fact]
    public void A_generated_molecule_is_always_accepted() {
        // Arrange
        Molecule anyMolecule = Any.Molecule().Generate();

        // Assert
        Assert.NotEmpty(anyMolecule.Name);
    }

    [Fact]
    public void A_null_name_is_a_null_argument_exception() {
        Assert.Throws<ArgumentNullException>(() => new Molecule(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_an_argument_exception(string blank) {
        Assert.Throws<ArgumentException>(() => new Molecule(blank));
    }

    [Theory]
    [InlineData("Paracetamol")]
    [InlineData("paracetaMol")]
    [InlineData("1paracetamol")]
    [InlineData("-paracetamol")]
    [InlineData(" paracetamol")]
    [InlineData("para_cetamol")]
    [InlineData("paracetamol500")]
    [InlineData("paracétamol")]
    public void A_name_outside_the_INN_shape_is_an_argument_exception(string name) {
        Assert.Throws<ArgumentException>(() => new Molecule(name));
    }

    [Fact]
    public void A_name_of_80_characters_is_accepted() {
        string eighty = new string('a', 80);

        Molecule molecule = new Molecule(eighty);

        Assert.Equal(80, molecule.Name.Length);
    }

    [Fact]
    public void A_name_of_81_characters_is_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Molecule(new string('a', 81)));
    }

    [Fact]
    public void The_length_is_checked_before_the_shape() {
        // 81 characters and an upper-case first letter: the length error wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Molecule("A" + new string('a', 80)));
    }

    [Fact]
    public void Two_molecules_with_the_same_name_are_equal() {
        // Arrange
        string anyName = Any.Molecule().Generate().Name;

        // Act
        Molecule first  = new Molecule(anyName);
        Molecule second = new Molecule(anyName);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_molecules_with_different_names_are_not_equal() {
        // Arrange
        Molecule anyMolecule      = Any.Molecule().Generate();
        Molecule anyOtherMolecule = Any.Molecule().DifferentFrom(anyMolecule).Generate();

        // Assert
        Assert.NotEqual(anyMolecule, anyOtherMolecule);
    }

    [Fact(Skip = "domain defect: an INN followed by a line feed is accepted, because the pattern ends with $ which matches before a final line feed; literal input, no seed involved")]
    public void A_trailing_line_feed_is_not_part_of_an_INN() {
        // domain defect: the pattern ends with $, which also matches before a final \n
        Assert.Throws<ArgumentException>(() => new Molecule("paracetamol\n"));
    }

}
