namespace InternationalCatalogue.Domain.Tests.Discovery;

/// <summary>
/// The tests' own implementation of the identifier check rules, written from the model so that a generator does
/// not lean on the code it helps to test.
/// </summary>
internal static class CheckCharacters {

    public static char Isbn13(string twelveDigits) {
        int sum = 0;
        for (int i = 0; i < 12; i++) {
            sum += (twelveDigits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }

    public static char Isbn10(string nineDigits) {
        int sum = 0;
        for (int i = 0; i < 9; i++) {
            sum += (nineDigits[i] - '0') * (10 - i);
        }

        int check = (11 - sum % 11) % 11;

        return check == 10 ? 'X' : (char)('0' + check);
    }

    public static char Issn(string sevenDigits) {
        int sum = 0;
        for (int i = 0; i < 7; i++) {
            sum += (sevenDigits[i] - '0') * (8 - i);
        }

        int check = (11 - sum % 11) % 11;

        return check == 10 ? 'X' : (char)('0' + check);
    }

}
