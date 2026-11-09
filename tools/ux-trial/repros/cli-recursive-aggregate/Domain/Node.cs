namespace Tree.Domain;

public sealed class Node {

    public Node(string name, IReadOnlyList<Node> children) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(children);
        Name     = name;
        Children = children;
    }

    public string            Name     { get; }
    public IReadOnlyList<Node> Children { get; }

}
