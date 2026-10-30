namespace Tree.Domain;

public sealed class Employee {

    public Employee(string name, Employee? manager) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name    = name;
        Manager = manager;
    }

    public string    Name    { get; }
    public Employee? Manager { get; }

}
