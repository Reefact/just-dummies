namespace Manufacturing.Domain;

/// <summary>The three shifts of a 3x8 rota; numbered from one as the plant's ERP does.</summary>
public enum ShiftKind {
    Morning   = 1,
    Afternoon = 2,
    Night     = 3
}
