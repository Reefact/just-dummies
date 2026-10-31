namespace RailwayInterlocking.Domain;

/// <summary>
/// The interlocking aggregate: owns the sections, points and route table of one area and decides
/// which routes may be set. Invariants: a section is locked into at most one route; a locked point
/// never moves; no two active routes conflict; a route is only set over clear, unlocked sections.
/// </summary>
public sealed class Interlocking
{
    private readonly Dictionary<SectionId, TrackSection> _sections;
    private readonly Dictionary<string, Point> _points;
    private readonly Dictionary<string, Route> _routes;
    private readonly Dictionary<string, RouteLock> _active = new(StringComparer.Ordinal);

    public Interlocking(string areaName, IEnumerable<TrackSection> sections, IEnumerable<Point> points, IEnumerable<Route> routes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(areaName);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(routes);

        _sections = new Dictionary<SectionId, TrackSection>();
        foreach (TrackSection section in sections)
        {
            ArgumentNullException.ThrowIfNull(section, nameof(sections));
            if (!_sections.TryAdd(section.Id, section))
            {
                throw new ArgumentException($"Section {section.Id} is declared twice.", nameof(sections));
            }
        }

        _points = new Dictionary<string, Point>(StringComparer.Ordinal);
        foreach (Point point in points)
        {
            ArgumentNullException.ThrowIfNull(point, nameof(points));
            if (!_sections.ContainsKey(point.Section))
            {
                throw new ArgumentException($"Point {point.Name} lies in unknown section {point.Section}.", nameof(points));
            }

            if (!_points.TryAdd(point.Name, point))
            {
                throw new ArgumentException($"Point {point.Name} is declared twice.", nameof(points));
            }
        }

        _routes = new Dictionary<string, Route>(StringComparer.Ordinal);
        foreach (Route route in routes)
        {
            ArgumentNullException.ThrowIfNull(route, nameof(routes));
            foreach (SectionId id in route.Sections)
            {
                if (!_sections.ContainsKey(id))
                {
                    throw new ArgumentException($"Route {route.Name} traverses unknown section {id}.", nameof(routes));
                }
            }

            foreach (PointSetting setting in route.PointSettings)
            {
                if (!_points.TryGetValue(setting.PointName, out Point? point))
                {
                    throw new ArgumentException($"Route {route.Name} sets unknown point {setting.PointName}.", nameof(routes));
                }

                if (!route.Sections.Contains(point.Section))
                {
                    throw new ArgumentException($"Route {route.Name} sets point {point.Name}, which lies outside its sections.", nameof(routes));
                }
            }

            if (!_routes.TryAdd(route.Name, route))
            {
                throw new ArgumentException($"Route {route.Name} is declared twice.", nameof(routes));
            }
        }

        AreaName = areaName;
    }

    public string AreaName { get; }

    public IReadOnlyCollection<RouteLock> ActiveRoutes => _active.Values;

    public TrackSection Section(SectionId id) =>
        _sections.TryGetValue(id, out TrackSection? section) ? section : throw new KeyNotFoundException($"Unknown section {id}.");

    public Point Point(string name) =>
        _points.TryGetValue(name, out Point? point) ? point : throw new KeyNotFoundException($"Unknown point {name}.");

    /// <summary>
    /// Sets and locks a route for a train: checks every condition first, then throws the points and
    /// locks the sections atomically. Refuses with <see cref="InterlockingRefusedException"/> otherwise.
    /// </summary>
    public RouteLock RequestRoute(string routeName, TrainId train, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(train);
        if (!_routes.TryGetValue(routeName, out Route? route))
        {
            throw new InterlockingRefusedException(RefusalReason.UnknownRoute, $"No route named {routeName}.");
        }

        if (_active.Values.Any(l => l.Train.Equals(train)))
        {
            throw new InterlockingRefusedException(RefusalReason.TrainAlreadyHasRoute, $"Train {train} already holds a route.");
        }

        RouteLock? conflicting = _active.Values.FirstOrDefault(l => l.Route.ConflictsWith(route) || l.Route.Name == route.Name);
        if (conflicting is not null)
        {
            throw new InterlockingRefusedException(RefusalReason.ConflictingRoute, $"Route {route.Name} conflicts with active route {conflicting.Route.Name}.");
        }

        foreach (SectionId id in route.Sections)
        {
            TrackSection section = _sections[id];
            if (section.IsOccupied)
            {
                throw new InterlockingRefusedException(RefusalReason.SectionOccupied, $"Section {id} is occupied.");
            }

            if (section.IsLocked)
            {
                throw new InterlockingRefusedException(RefusalReason.SectionLocked, $"Section {id} is locked by route {section.LockedByRoute}.");
            }
        }

        foreach (PointSetting setting in route.PointSettings)
        {
            Point point = _points[setting.PointName];
            if (point.IsLocked && point.Position != setting.Required)
            {
                throw new InterlockingRefusedException(RefusalReason.PointLocked, $"Point {point.Name} is locked {point.Position} by route {point.LockedByRoute}.");
            }
        }

        foreach (PointSetting setting in route.PointSettings)
        {
            Point point = _points[setting.PointName];
            point.Throw(setting.Required);
            point.LockInto(route.Name);
        }

        foreach (SectionId id in route.Sections)
        {
            _sections[id].LockInto(route.Name);
        }

        RouteLock routeLock = new(route, train, at);
        _active.Add(route.Name, routeLock);
        return routeLock;
    }

    /// <summary>Cancels a route before its train has entered it. Refused once the train is on the route.</summary>
    public void CancelRoute(string routeName)
    {
        if (!_active.TryGetValue(routeName, out RouteLock? routeLock))
        {
            throw new InterlockingRefusedException(RefusalReason.RouteNotSet, $"Route {routeName} is not set.");
        }

        if (routeLock.TrainHasEntered)
        {
            throw new InterlockingRefusedException(RefusalReason.ApproachLocked, $"Train {routeLock.Train} has entered route {routeName}; it can only release behind the train.");
        }

        foreach (SectionId id in routeLock.RemainingSections)
        {
            _sections[id].Unlock();
        }

        ReleasePoints(routeLock.Route);
        _active.Remove(routeName);
    }

    /// <summary>A track circuit reports a train. Events may arrive in any order; occupancy is always recorded.</summary>
    public void OnSectionOccupied(SectionId id)
    {
        TrackSection section = Section(id);
        section.MarkOccupied();
        if (section.LockedByRoute is { } routeName && _active.TryGetValue(routeName, out RouteLock? routeLock))
        {
            routeLock.MarkVisited(id);
        }
    }

    /// <summary>
    /// A track circuit reports clear. Releases, from the rear, every section the train has occupied and
    /// left; releases the route's points once no remaining section holds them; ends the route when fully
    /// released. A clear ahead of the train, or on a section never occupied, keeps the lock.
    /// </summary>
    public void OnSectionCleared(SectionId id)
    {
        TrackSection section = Section(id);
        section.MarkClear();
        if (section.LockedByRoute is not { } routeName || !_active.TryGetValue(routeName, out RouteLock? routeLock))
        {
            return;
        }

        // Sequential release: free the rear while the train has passed through it and left it,
        // which also catches up on sections whose clear arrived before the one behind them.
        while (routeLock.RemainingSections.Count > 0)
        {
            SectionId rear = routeLock.RemainingSections[0];
            TrackSection rearSection = _sections[rear];
            if (!routeLock.HasVisited(rear) || rearSection.IsOccupied)
            {
                break;
            }

            routeLock.ReleaseRear(rear);
            rearSection.Unlock();
        }

        foreach (PointSetting setting in routeLock.Route.PointSettings)
        {
            Point point = _points[setting.PointName];
            if (point.LockedByRoute == routeName && !routeLock.RemainingSections.Contains(point.Section))
            {
                point.Unlock();
            }
        }

        if (routeLock.IsFullyReleased)
        {
            _active.Remove(routeName);
        }
    }

    private void ReleasePoints(Route route)
    {
        foreach (PointSetting setting in route.PointSettings)
        {
            Point point = _points[setting.PointName];
            if (point.LockedByRoute == route.Name)
            {
                point.Unlock();
            }
        }
    }
}
