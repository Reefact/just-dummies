namespace Jd.Domain;

public enum Currency { Eur = 1, Usd = 2 }

[System.Flags]
public enum Access { None = 0, Read = 1, Write = 2 }
