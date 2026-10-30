namespace Aviation.Domain;

public enum FlightStatus {
    Scheduled,
    Boarding,
    Departed,
    Arrived,
    Cancelled
}

public enum DelayCategory {
    OnTime,
    Minor,
    Significant,
    Major
}
