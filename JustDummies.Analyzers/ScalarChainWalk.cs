using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     One chain's constraints, walked in the order they were written over the domain they narrow: JD023 reports the
///     first that empties it, JD024 each that narrows nothing, JD038 the chain that ends on exactly one value.
/// </summary>
/// <remarks>
///     <para>
///         A time of day has one more thing to mirror. The first <c>Between(start, end)</c> whose start comes after
///         its end moves the generator to a frame that starts the day at <c>start</c>, where the window is one
///         interval, and every earlier declaration is applied again there; a later bound is read along the window,
///         and a time outside it admits no value (ADR-0125). The walk does the same: it re-reads the earlier calls in
///         the frame when it meets the window, reports a refusal there on the window as the run time does, and reads
///         every call after it in the frame.
///     </para>
///     <para>
///         The walk ends at the first call it cannot read — an argument that does not fold, a constraint the model
///         does not track, a declaration another rule owns — rather than guessing: a rule that claims a chain is
///         unsatisfiable, or reduced to one value, must be certain.
///     </para>
/// </remarks>
internal sealed class ScalarChainWalk {

    private const long Day = TimeSpan.TicksPerDay;

    private readonly OperationAnalysisContext            _context;
    private readonly ScalarFamily                        _family;
    private readonly IReadOnlyList<IInvocationOperation> _constraints;

    // The calls walked so far as the run time renders them, and the ones holding each bound: a conflict names the
    // constraint it collides with, the way the exception the chain would throw does (issue #322).
    private readonly List<string>                                       _written  = [];
    private readonly HashSet<string>                                    _seen     = [];
    private readonly List<(string Name, IReadOnlyList<decimal> Values)> _declared = [];
    private IScalarDomain                                               _state;
    private string?                                                     _minimumSetBy;
    private string?                                                     _maximumSetBy;

    /// <summary>The first call after which exactly one value is left, which is where JD038 points.</summary>
    private IInvocationOperation? _pinnedBy;

    /// <summary>Whether the chain pins its value by name — <c>Zero()</c> — which JD038 leaves alone.</summary>
    private bool _pinnedByName;

    /// <summary>Whether a constraint narrowing nothing is reported: not while the earlier calls are read again in a frame.</summary>
    private bool _reportingInert = true;

    // The frame a time of day is read in once a window crosses midnight: where the day starts, and how long the window is.
    private long? _frameStart;
    private long  _frameLength;
    private string _window = "";

    public ScalarChainWalk(OperationAnalysisContext context, ScalarFamily family, IReadOnlyList<IInvocationOperation> constraints) {
        _context     = context;
        _family      = family;
        _constraints = constraints;
        _state       = family.Unconstrained(ordinal => ordinal);
    }

    public void Run() {
        int? crossing = _family.Kind == TemporalKind.TimeOnly ? IndexOfCrossingWindow() : null;

        for (int index = 0; index < _constraints.Count; index++) {
            bool walked = index == crossing ? CrossMidnight(_constraints[index]) : Step(_constraints[index], _constraints[index]);
            if (!walked) { return; }
        }

        ReportTheOnlyValue();
    }

    /// <summary>
    ///     Applies one call, reporting what it empties or leaves unchanged; <c>false</c> once the walk is over — the
    ///     chain was reported, or it cannot be read further. <paramref name="reportedAt" /> is where an emptiness is
    ///     reported: the call itself, or the window the call is applied again under.
    /// </summary>
    private bool Step(IInvocationOperation constraint, IInvocationOperation reportedAt) {
        if (!TryReadCall(constraint, out string name, out List<(decimal Value, string Rendered)> arguments)) { return false; }

        // The draw at the end of the chain declares nothing: the walk reads past it to its verdict.
        if (name == "Generate" && arguments.Count == 0) { return true; }
        // An empty exclusion is reported per call by the analyzer, whatever the chain, and changes no state here.
        if (name == "Except" && arguments.Count == 0) { return true; }
        if (name == "Zero") { _pinnedByName = true; }
        if (_frameStart is not null && !HoldsInTheWindow(constraint, reportedAt, name, arguments)) { return false; }

        List<decimal> values = arguments.Select(argument => argument.Value).ToList();

        // The exclusion that removes nothing: silent at run time, and the reason JD024 exists. The same exclusion
        // written twice is JD035's, as a repetition in every generator family (issue #279).
        if (IsInertExclusion(name, values)) {
            ReportInert(constraint, $"{name} removes no value the generator could produce");
            _declared.Add((name, values));

            return true;
        }

        IScalarDomain? next = _state.Apply(Lattice(name, values), values);
        if (next is null) { return false; }

        string call = Render(name, arguments);
        if (next.IsEmpty()) {
            Report(Descriptors.ScalarChainAdmitsNoValue, reportedAt, call, Conflict(next));

            return false;
        }

        // A bound the chain already named is JD032's, not this rule's: the same NAME twice is one phenomenon,
        // reported once, in both writing orders and in every generator family (ADR-0078). JD024 keeps what its
        // message actually describes -- a constraint that narrows nothing -- which still covers a bound implied
        // by a DIFFERENT one, Positive() after GreaterThan(5) being the shape it exists for.
        if (IsNarrowingConstraint(name) && _state.NarrowsNothing(next) && !IsSecondDeclarationOfTheSameBound(name, values)) {
            ReportInert(constraint, $"{name} is already implied by the constraints declared before it");
        }

        Advance(constraint, name, values, call, next);

        return true;
    }

    /// <summary>Moves the walk past <paramref name="constraint" />, keeping what a later message or verdict needs of it.</summary>
    private void Advance(IInvocationOperation constraint, string name, List<decimal> values, string call, IScalarDomain next) {
        _seen.Add(name);
        _declared.Add((name, values));
        _written.Add(call);
        if (next.TightensMinimumOf(_state)) { _minimumSetBy = call; }
        if (next.TightensMaximumOf(_state)) { _maximumSetBy = call; }
        _state = next;
        if (_pinnedBy is null && next.CountAdmitted(1) == 1) { _pinnedBy = constraint; }
    }

    /// <summary>
    ///     Reads a call as the model takes it: its name as written, and its constant arguments with the text the run
    ///     time renders each with — a granularity read as the ticks of its <c>TimeSpan</c>. <c>false</c> for an
    ///     argument that does not fold.
    /// </summary>
    private bool TryReadCall(IInvocationOperation constraint, out string name, out List<(decimal Value, string Rendered)> arguments) {
        name      = constraint.TargetMethod.Name;
        arguments = [];
        if (name == "WithGranularity") { return TryReadGranularity(constraint, arguments); }

        foreach (IArgumentOperation argument in constraint.Arguments) {
            bool read = argument.ArgumentKind == ArgumentKind.ParamArray ? TryReadElements(argument.Value, arguments) : TryReadArgument(argument.Value, arguments);
            if (!read) { return false; }
        }

        return true;
    }

    /// <summary>A granularity, as the ticks of its <c>TimeSpan</c>; one the guard refuses is JD014's, and the walk stands down.</summary>
    private bool TryReadGranularity(IInvocationOperation constraint, List<(decimal Value, string Rendered)> arguments) {
        if (_family.Kind is null || constraint.Arguments.Length != 1 || !ConstantFacts.TryGetTimeSpanTicks(constraint.Arguments[0].Value, out long ticks) || ticks <= 0) { return false; }

        arguments.Add((ticks, TemporalFacts.Render(TemporalKind.TimeSpan, ticks)));

        return true;
    }

    /// <summary>The elements of a value set or an exclusion list written inline, each a constant of the family's type.</summary>
    private bool TryReadElements(IOperation list, List<(decimal Value, string Rendered)> arguments) {
        return list is IArrayCreationOperation { Initializer: { } initializer } && initializer.ElementValues.All(element => TryReadArgument(element, arguments));
    }

    /// <summary>Reads one constant of the family's type onto <paramref name="arguments" />, or answers <c>false</c>.</summary>
    private bool TryReadArgument(IOperation operation, List<(decimal Value, string Rendered)> arguments) {
        if (!TryReadArgument(operation, out (decimal Value, string Rendered) read)) { return false; }

        arguments.Add(read);

        return true;
    }

    /// <summary>A constant of the family's type, its ordinal moved into the frame a time of day is read in.</summary>
    private bool TryReadArgument(IOperation operation, out (decimal Value, string Rendered) read) {
        read = default;
        if (_family.Read(operation) is not (decimal value, string rendered)) { return false; }

        read = (_frameStart is long start ? ((long)value + Day - start) % Day : value, rendered);

        return true;
    }

    /// <summary>
    ///     The lattice a call declares, named for the model: a granularity is a step anchored at the type's origin —
    ///     and, in a frame that starts the day elsewhere, at the residue that origin leaves, which exists only for a
    ///     granularity that divides a day; the run time refuses the others on its own, and the walk stands down.
    /// </summary>
    private string Lattice(string name, List<decimal> values) {
        if (name != "WithGranularity") { return ScalarFamily.Normalize(name); }

        long step = (long)values[0];
        if (_frameStart is not long start) {
            values.Add(0);

            return "OnLattice";
        }

        if (Day % step != 0) {
            values.Clear();

            return name;
        }

        values.Add(((start % step) + step) % step == 0 ? 0 : step - start % step);

        return "OnLattice";
    }

    /// <summary>
    ///     What the run time checks in a frame before it applies a declaration: a bound outside the window admits no
    ///     value, and a window whose end comes first along the frame would leave two pieces (ADR-0125). Reported on the
    ///     call, or on the window when the call is applied again under it, worded as the run time words each.
    /// </summary>
    private bool HoldsInTheWindow(IInvocationOperation constraint, IInvocationOperation reportedAt, string name, List<(decimal Value, string Rendered)> arguments) {
        if (!IsBound(name)) { return true; }

        bool   onTheWindow = !ReferenceEquals(constraint, reportedAt);
        string window      = _window;
        string call        = Render(name, arguments);

        foreach ((decimal value, string rendered) in arguments) {
            if (value <= _frameLength) { continue; }

            Report(Descriptors.ScalarChainAdmitsNoValue, reportedAt, onTheWindow ? window : call,
                   onTheWindow ? $"{call} declares {rendered}, outside the window it declares across midnight" : $"{rendered} lies outside the window {window} declares across midnight");

            return false;
        }

        if (arguments.Count == 2 && arguments[0].Value > arguments[1].Value) {
            Report(Descriptors.ScalarChainAdmitsNoValue, reportedAt, onTheWindow ? window : call,
                   onTheWindow
                       ? $"{call} ends before it starts along the window it declares across midnight, which leaves two pieces rather than one window"
                       : $"{arguments[1].Rendered} comes before {arguments[0].Rendered} along the window {window} declares across midnight, which leaves two pieces rather than one window");

            return false;
        }

        return true;
    }

    /// <summary>
    ///     Moves the walk to the frame the window opens, and applies every earlier call again there, then the window
    ///     itself, as the run time does when it meets the first window across midnight.
    /// </summary>
    private bool CrossMidnight(IInvocationOperation window) {
        if (!TryReadCall(window, out _, out List<(decimal Value, string Rendered)> ends)) { return false; }

        long start = (long)ends[0].Value;
        long end   = (long)ends[1].Value;
        _window      = Render("Between", ends);
        _frameStart  = start;
        _frameLength = (end + Day - start) % Day;
        _state       = _family.Unconstrained(ordinal => (ordinal + start) % Day);
        _written.Clear();
        _seen.Clear();
        _declared.Clear();
        _minimumSetBy = null;
        _maximumSetBy = null;
        _pinnedBy     = null;

        _reportingInert = false;
        if (!_constraints.TakeWhile(constraint => !ReferenceEquals(constraint, window)).All(earlier => Step(earlier, window))) { return false; }

        _reportingInert = true;

        return Step(window, window);
    }

    /// <summary>The first <c>Between</c> whose start comes after its end, read on the line: the window that opens the frame.</summary>
    private int? IndexOfCrossingWindow() {
        for (int index = 0; index < _constraints.Count; index++) {
            IInvocationOperation constraint = _constraints[index];
            if (constraint.TargetMethod.Name != "Between" || !TryReadCall(constraint, out _, out List<(decimal Value, string Rendered)> ends) || ends.Count != 2) { continue; }
            if (ends[0].Value > ends[1].Value) { return index; }
        }

        return null;
    }

    /// <summary>
    ///     Why the domain admits nothing, worded as the exception the chain throws words it: two bounds that cross
    ///     name the bound the call collides with and what it already requires, or the type when the colliding edge is
    ///     the type's own. The library tracks exclusions, a lattice and an allow-list in more detail than this model
    ///     does, so a conflict among those names every constraint written before the call rather than guessing which
    ///     one the run time would single out.
    /// </summary>
    private string Conflict(IScalarDomain next) {
        if (next.Saturated || next.BoundsCross) {
            if (next.TightensMinimumOf(_state) && _maximumSetBy is not null) { return $"{_maximumSetBy} already requires values {_state.MaximumRequirement}"; }
            if (next.TightensMaximumOf(_state) && _minimumSetBy is not null) { return $"{_minimumSetBy} already requires values {_state.MinimumRequirement}"; }

            return $"no {_family.Name} value satisfies it";
        }

        return _written.Count == 0
                   ? $"no {_family.Name} value satisfies it"
                   : $"no value admitted by {string.Join(", ", _written)} survives it";
    }

    /// <summary>
    ///     JD038: the chain ends on exactly one value, which is a literal dressed as a draw. Not for a value pinned by
    ///     name, which already reads as the constant it is.
    /// </summary>
    private void ReportTheOnlyValue() {
        if (_pinnedBy is null || _pinnedByName || _state.TheOnlyValue() is not string value) { return; }

        string wholeDay = _family.Kind == TemporalKind.TimeOnly ? ", or Any.TimeOnly() if the whole day was meant" : "";
        Report(Descriptors.ScalarChainAdmitsOneValue, _pinnedBy, value, wholeDay);
    }

    private void ReportInert(IInvocationOperation constraint, string reason) {
        if (_reportingInert) { Report(Descriptors.ConstraintWithNoEffect, constraint, reason); }
    }

    private void Report(DiagnosticDescriptor descriptor, IInvocationOperation at, params object[] arguments) {
        _context.ReportDiagnostic(Diagnostic.Create(descriptor, at.Syntax.GetLocation(), arguments));
    }

    // A constraint as ConstraintCall renders it at run time: its name, then its arguments between parentheses.
    private static string Render(string name, List<(decimal Value, string Rendered)> arguments) {
        return $"{name}({string.Join(", ", arguments.Select(argument => argument.Rendered))})";
    }

    private bool IsInertExclusion(string name, List<decimal> values) {
        return name is "Except" or "DifferentFrom"
            && _state.ExclusionIsInert(values)
            && !_declared.Any(earlier => earlier.Name == name && new HashSet<decimal>(earlier.Values).SetEquals(values));
    }

    /// <summary>Whether JD032 owns this call: a single-argument folding bound whose name the chain already used.</summary>
    private bool IsSecondDeclarationOfTheSameBound(string name, List<decimal> values) {
        return values.Count == 1 && BoundDeclaredTwiceAnalyzer.FoldingBounds.Contains(name) && _seen.Contains(name);
    }

    // A bound whose job is to narrow. Applying one that changes nothing is what JD024 reports; Positive() after
    // GreaterThan(5) is the shape, and it reads as a tightening that is not one.
    private static bool IsNarrowingConstraint(string name) {
        return IsBound(name) || name is "Positive" or "Negative";
    }

    private static bool IsBound(string name) {
        return name is "GreaterThan" or "GreaterThanOrEqualTo" or "LessThan" or "LessThanOrEqualTo" or "Between"
                    or "After" or "AfterOrEqualTo" or "Before" or "BeforeOrEqualTo";
    }

}
