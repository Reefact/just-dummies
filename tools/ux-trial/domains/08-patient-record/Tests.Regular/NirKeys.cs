namespace Clinical.Domain;

/// <summary>An independent computation of the NIR control key, so generators do not lean on the code under test.</summary>
public static class NirKeys {

    public static int KeyOf(string body) {
        long n = long.Parse(body.Replace("2A", "19").Replace("2B", "18"));

        return (int)(97 - n % 97);
    }

}
