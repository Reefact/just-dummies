namespace Zone.Domain;

public sealed class Tolerance {

    public Tolerance(string zone) {
        if (zone is not ("H" or "h" or "JS" or "js")) {
            throw new ArgumentException($"Unsupported zone '{zone}'.", nameof(zone));
        }
        Zone = zone;
    }

    public string Zone { get; }

}
