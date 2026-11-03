using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class IdentifierTests {

    // ---- Isbn13 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("9791090636071")]
    [InlineData("9781234567897")]
    public void A_well_formed_ISBN_13_is_accepted_as_written(string written) {
        // Act
        Isbn13 isbn = new Isbn13(written);

        // Assert
        Assert.Equal(written, isbn.Value);
        Assert.Equal(written, isbn.ToString());
    }

    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("979-10-90636-07-1", "9791090636071")]
    [InlineData("9-7-8-0-3-0-6-4-0-6-1-5-7", "9780306406157")]
    public void Hyphens_between_groups_are_removed(string written, string stored) {
        // Act
        Isbn13 isbn = new Isbn13(written);

        // Assert
        Assert.Equal(stored, isbn.Value);
    }

    [Theory]
    [InlineData("-9780306406157")]
    [InlineData("9780306406157-")]
    [InlineData("978--0306406157")]
    [InlineData("-")]
    [InlineData("--")]
    public void A_leading_trailing_or_doubled_hyphen_is_an_argument_error(string written) {
        Assert.Throws<ArgumentException>(() => new Isbn13(written));
    }

    [Theory]
    [InlineData("")]
    [InlineData("978030640615")]
    [InlineData("97803064061577")]
    [InlineData("978030640615X")]
    [InlineData("978 0306406157")]
    [InlineData("９７８０３０６４０６１５７")]
    [InlineData("978\u200903064061")]
    [InlineData("9780306406157\n")]
    public void Something_other_than_thirteen_ASCII_digits_is_refused(string written) {
        Assert.Throws<FormatException>(() => new Isbn13(written));
    }

    [Theory]
    [InlineData("9780306406150", "7")]
    [InlineData("9780306406158", "7")]
    [InlineData("9791090636070", "1")]
    public void A_wrong_check_digit_is_refused_and_the_message_names_the_expected_one(string written, string expected) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Isbn13(written));

        // Assert
        Assert.Contains("should be " + expected, refusal.Message);
    }

    [Fact]
    public void A_prefix_other_than_978_or_979_is_refused_even_with_a_valid_check_digit() {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Isbn13("9771234567003"));

        // Assert
        Assert.Contains("978 or 979", refusal.Message);
    }

    [Fact]
    public void A_null_ISBN_13_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Isbn13(null!));
    }

    [Theory]
    [InlineData("978030640615", 7)]
    [InlineData("979109063607", 1)]
    [InlineData("978000000020", 0)]
    public void The_check_digit_is_computed_with_weights_one_and_three(string twelveDigits, int expected) {
        Assert.Equal(expected, Isbn13.ComputeCheckDigit(twelveDigits));
    }

    [Theory]
    [InlineData("97803064061")]
    [InlineData("9780306406157")]
    [InlineData("")]
    public void The_check_digit_needs_exactly_twelve_digits(string digits) {
        Assert.Throws<ArgumentException>(() => Isbn13.ComputeCheckDigit(digits));
    }

    [Fact]
    public void Only_a_978_ISBN_has_an_ISBN_10_form() {
        // Assert
        Assert.True(new Isbn13("9780306406157").HasIsbn10Form);
        Assert.False(new Isbn13("9791090636071").HasIsbn10Form);
    }

    [Fact]
    public void A_978_ISBN_converts_to_its_ISBN_10() {
        Assert.Equal("0306406152", new Isbn13("978-0-306-40615-7").ToIsbn10().Value);
    }

    [Fact]
    public void A_979_ISBN_has_no_ISBN_10() {
        // Arrange
        Isbn13 anyNewPrefixIsbn = Any.Isbn13().With979Prefix().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyNewPrefixIsbn.ToIsbn10());
    }

    [Fact]
    public void An_ISBN_13_whose_check_digit_is_X_in_its_ISBN_10_converts_back_and_forth() {
        // Arrange: 978-0-8044-2957-X
        Isbn10 legacy = new Isbn10("080442957X");

        // Act
        Isbn13 modern = legacy.ToIsbn13();

        // Assert
        Assert.Equal("9780804429573", modern.Value);
        Assert.Equal(legacy, modern.ToIsbn10());
    }

    [Fact]
    public void ISBN_13s_are_equal_when_their_digits_are() {
        // Assert
        Assert.Equal(new Isbn13("978-0-306-40615-7"), new Isbn13("9780306406157"));
        Assert.Equal(new Isbn13("978-0-306-40615-7").GetHashCode(), new Isbn13("9780306406157").GetHashCode());
        Assert.NotEqual(new Isbn13("9780306406157"), new Isbn13("9791090636071"));
    }

    // ---- Isbn10 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("0306406152", "0306406152")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("080442957X", "080442957X")]
    [InlineData("080442957x", "080442957X")]
    [InlineData("0-8044-2957-x", "080442957X")]
    [InlineData("0000000000", "0000000000")]
    public void A_well_formed_ISBN_10_is_accepted_and_stored_without_hyphens_and_in_upper_case(string written, string stored) {
        // Act
        Isbn10 isbn = new Isbn10(written);

        // Assert
        Assert.Equal(stored, isbn.Value);
        Assert.Equal(stored, isbn.ToString());
    }

    [Theory]
    [InlineData("-0306406152")]
    [InlineData("0306406152-")]
    [InlineData("0--306406152")]
    public void Hyphen_misuse_in_an_ISBN_10_is_an_argument_error(string written) {
        Assert.Throws<ArgumentException>(() => new Isbn10(written));
    }

    [Theory]
    [InlineData("")]
    [InlineData("030640615")]
    [InlineData("03064061522")]
    [InlineData("030640615 ")]
    [InlineData("03064061X2")]
    [InlineData("X306406152")]
    [InlineData("030640615Y")]
    [InlineData("0306406150")]
    [InlineData("0306406153")]
    [InlineData("080442957 ")]
    [InlineData("0804429570")]
    [InlineData("０３０６４０６１５２")]
    public void A_malformed_or_miscomputed_ISBN_10_is_refused(string written) {
        Assert.Throws<FormatException>(() => new Isbn10(written));
    }

    [Fact]
    public void A_check_character_of_X_stands_for_ten() {
        // Assert
        Assert.Equal('X', Isbn10.ComputeCheckCharacter("080442957"));
        Assert.Equal('2', Isbn10.ComputeCheckCharacter("030640615"));
        Assert.Equal('0', Isbn10.ComputeCheckCharacter("000000000"));
    }

    [Fact]
    public void The_ISBN_10_check_character_needs_exactly_nine_digits() {
        Assert.Throws<ArgumentException>(() => Isbn10.ComputeCheckCharacter("0306406152"));
    }

    [Fact]
    public void An_ISBN_10_converts_to_an_ISBN_13_with_a_recomputed_check_digit() {
        Assert.Equal("9780306406157", new Isbn10("0306406152").ToIsbn13().Value);
    }

    [Fact]
    public void A_null_ISBN_10_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Isbn10(null!));
    }

    [Fact]
    public void ISBN_10s_are_equal_when_their_stored_values_are() {
        // Assert
        Assert.Equal(new Isbn10("080442957x"), new Isbn10("0-8044-2957-X"));
        Assert.NotEqual(new Isbn10("0306406152"), new Isbn10("080442957X"));
    }

    // ---- Issn ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("0378-5955")]
    [InlineData("2434-561X")]
    [InlineData("0000-0000")]
    public void A_well_formed_ISSN_is_accepted(string written) {
        // Act
        Issn issn = new Issn(written);

        // Assert
        Assert.Equal(written, issn.Value);
        Assert.Equal(written, issn.ToString());
    }

    [Theory]
    [InlineData("03785955")]
    [InlineData("0378-595")]
    [InlineData("0378-59555")]
    [InlineData("0378 5955")]
    [InlineData("0378-5950")]
    [InlineData("0378-595X")]
    [InlineData("2434-561x")]
    [InlineData("2434-5610")]
    [InlineData("X378-5955")]
    [InlineData("0378-5955\n")]
    [InlineData(" 0378-5955")]
    [InlineData("０３７８-５９５５")]
    [InlineData("")]
    public void A_malformed_or_miscomputed_ISSN_is_refused(string written) {
        Assert.Throws<FormatException>(() => new Issn(written));
    }

    [Fact]
    public void The_ISSN_check_character_is_computed_over_seven_digits_with_X_for_ten() {
        // Assert
        Assert.Equal('5', Issn.ComputeCheckCharacter("0378595"));
        Assert.Equal('X', Issn.ComputeCheckCharacter("2434561"));
        Assert.Equal('0', Issn.ComputeCheckCharacter("0000000"));
    }

    [Fact]
    public void A_null_ISSN_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Issn(null!));
    }

    [Fact]
    public void ISSNs_are_equal_when_their_values_are() {
        // Assert
        Assert.Equal(new Issn("0378-5955"), new Issn("0378-5955"));
        Assert.NotEqual(new Issn("0378-5955"), new Issn("2434-561X"));
    }

    // ---- CallNumber ---------------------------------------------------------------------------

    [Fact]
    public void A_call_number_is_read_into_its_class_and_Cutter_parts() {
        // Act
        CallNumber callNumber = new CallNumber("823.914 R884");

        // Assert
        Assert.Equal(823.914m, callNumber.ClassNumber);
        Assert.Equal('R', callNumber.CutterLetter);
        Assert.Equal(0.884m, callNumber.CutterFraction);
        Assert.Equal("823.914 R884", callNumber.Value);
        Assert.Equal("823.914 R884", callNumber.ToString());
    }

    [Theory]
    [InlineData("100 A1")]
    [InlineData("000 A1")]
    [InlineData("999.99999999 Z9999")]
    [InlineData("005.1 B0")]
    [InlineData("005.10 B01")]
    public void A_call_number_within_the_documented_shape_is_accepted(string written) {
        Assert.Equal(written, new CallNumber(written).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("82 R884")]
    [InlineData("8234 R884")]
    [InlineData("823. R884")]
    [InlineData("823.914R884")]
    [InlineData("823.914  R884")]
    [InlineData("823.914 r884")]
    [InlineData("823.914 R")]
    [InlineData("823.914 R88444")]
    [InlineData("823.914 RR884")]
    [InlineData("823.123456789 R1")]
    [InlineData(".914 R884")]
    [InlineData("823,914 R884")]
    [InlineData("823.914 R884\n")]
    [InlineData(" 823.914 R884")]
    [InlineData("８２３ R884")]
    [InlineData("823 É12")]
    public void A_call_number_outside_the_documented_shape_is_refused(string written) {
        Assert.Throws<FormatException>(() => new CallNumber(written));
    }

    [Fact]
    public void A_null_call_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CallNumber(null!));
    }

    [Fact]
    public void The_Cutter_digits_are_ordered_as_a_decimal_fraction() {
        // Arrange
        CallNumber r884 = new CallNumber("823.914 R884");
        CallNumber r89  = new CallNumber("823.914 R89");
        CallNumber r9   = new CallNumber("823.914 R9");
        CallNumber r88  = new CallNumber("823.914 R88");

        // Assert
        Assert.True(r88.CompareTo(r884) < 0);
        Assert.True(r884.CompareTo(r89) < 0);
        Assert.True(r89.CompareTo(r9) < 0);
        Assert.True(r9.CompareTo(r884) > 0);
    }

    [Fact]
    public void Call_numbers_are_ordered_by_class_then_Cutter_letter_then_Cutter_fraction() {
        // Arrange
        string[] shelfOrder = ["005 Z9", "823 A1", "823 B1", "823.5 A1", "823.914 A1", "823.914 B1", "823.914 B2", "824 A1"];

        // Act
        List<string> shuffled = shelfOrder.Reverse().ToList();
        shuffled.Sort((left, right) => new CallNumber(left).CompareTo(new CallNumber(right)));

        // Assert
        Assert.Equal(shelfOrder, shuffled);
    }

    [Fact]
    public void Call_numbers_with_the_same_numeric_value_fall_back_to_ordinal_order() {
        // Arrange
        CallNumber shorter = new CallNumber("823.9 R1");
        CallNumber longer  = new CallNumber("823.90 R1");

        // Assert
        Assert.True(shorter.CompareTo(longer) < 0);
        Assert.True(longer.CompareTo(shorter) > 0);
        Assert.False(shorter.Equals(longer));
    }

    [Fact]
    public void A_call_number_compares_equal_to_itself_and_after_null() {
        // Arrange
        CallNumber anyCallNumber = Any.CallNumber().Generate();

        // Assert
        Assert.Equal(0, anyCallNumber.CompareTo(new CallNumber(anyCallNumber.Value)));
        Assert.True(anyCallNumber.CompareTo(null) > 0);
        Assert.Equal(anyCallNumber, new CallNumber(anyCallNumber.Value));
    }

    // ---- RecordId -----------------------------------------------------------------------------

    [Fact]
    public void A_record_id_names_its_agency() {
        // Act
        RecordId id = new RecordId("FR00412873");

        // Assert
        Assert.Equal("FR00412873", id.Value);
        Assert.Equal("FR00412873", id.ToString());
        Assert.Equal("FR", id.Agency);
    }

    [Theory]
    [InlineData("AA00000001")]
    [InlineData("ZZ99999999")]
    [InlineData("XX10000000")]
    public void The_extreme_numbers_are_accepted(string written) {
        Assert.Equal(written, new RecordId(written).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FR0041287")]
    [InlineData("FR004128733")]
    [InlineData("fr00412873")]
    [InlineData("Fr00412873")]
    [InlineData("F100412873")]
    [InlineData("1R00412873")]
    [InlineData("ÉR00412873")]
    [InlineData("FR0041287X")]
    [InlineData("FR-0412873")]
    [InlineData("FR٠٠٤١٢٨٧٣")]
    [InlineData("FR00412873 ")]
    public void A_malformed_record_id_is_refused(string written) {
        Assert.Throws<FormatException>(() => new RecordId(written));
    }

    [Fact]
    public void An_all_zero_record_number_is_refused() {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new RecordId("FR00000000"));

        // Assert
        Assert.Contains("zero", refusal.Message);
    }

    [Fact]
    public void A_null_record_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RecordId(null!));
    }

    [Fact]
    public void Record_ids_are_equal_when_their_values_are() {
        // Assert
        Assert.Equal(new RecordId("FR00412873"), new RecordId("FR00412873"));
        Assert.Equal(new RecordId("FR00412873").GetHashCode(), new RecordId("FR00412873").GetHashCode());
        Assert.NotEqual(new RecordId("FR00412873"), new RecordId("DE00412873"));
    }

}
