namespace NullRepro;

public sealed class Bic {
    public Bic(string code) {
        if (code.Length != 8) {
            throw new ArgumentException("A BIC has 8 characters.", nameof(code));
        }
        Code = code;
    }

    public string Code { get; }
}

public sealed class Party {
    public Party(string name, Bic? agent, string? note, int? priority) {
        Name = name;
        Agent = agent;
        Note = note;
        Priority = priority;
    }

    public string Name { get; }
    public Bic? Agent { get; }
    public string? Note { get; }
    public int? Priority { get; }
}
