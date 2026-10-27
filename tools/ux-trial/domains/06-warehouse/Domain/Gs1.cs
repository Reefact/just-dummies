namespace Warehouse.Domain;

/// <summary>The GS1 mod-10 check digit shared by SSCC, GTIN-13 and every other GS1 key.</summary>
public static class Gs1 {

    public static int ComputeCheckDigit(string digitsWithoutCheck) {
        int sum = 0;
        for (int index = 0; index < digitsWithoutCheck.Length; index++) {
            int digit = digitsWithoutCheck[digitsWithoutCheck.Length - 1 - index] - '0';
            sum += index % 2 == 0 ? digit * 3 : digit;
        }

        return (10 - sum % 10) % 10;
    }

    public static bool HasValidCheckDigit(string digits) {
        int expected = ComputeCheckDigit(digits[..^1]);

        return digits[^1] - '0' == expected;
    }

}
