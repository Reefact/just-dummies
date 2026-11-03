namespace PassiveComponents.Domain;

public sealed partial class AnyPart {

    public AnyPart Capacitor() => WithKind(ComponentKind.Capacitor);

    public AnyPart Inductor() => WithKind(ComponentKind.Inductor);

    public AnyPart Resistor() => WithKind(ComponentKind.Resistor);

}
