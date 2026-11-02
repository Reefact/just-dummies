// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EmploymentContract --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EmploymentContract" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmploymentContract : IAny<EmploymentContract> {

    private IAny<EmployeeId>   _employee;
    private IAny<ContractType> _type;
    private IAny<DateOnly>     _hiredOn;
    private IAny<DateOnly?>?   _endsOn;
    private IAny<int>          _lengthInDays;
    private IAny<HourlyRate>   _rate;
    private IAny<int>          _weeklyHours;

    private AnyEmployeeId         _employeeRecipe;
    private AnyEnum<ContractType> _typeRecipe;
    private AnyDateOnly           _hiredOnRecipe;
    private AnyInt32              _lengthInDaysRecipe;
    private AnyHourlyRate         _rateRecipe;
    private AnyInt32              _weeklyHoursRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmploymentContract()
        : this(employee:    AnyValidEmployee(),
               type:        AnyValidType(),
               hiredOn:     AnyValidHiredOn(),
               lengthInDays: AnyValidLengthInDays(),
               rate:        AnyValidRate(),
               weeklyHours: AnyValidWeeklyHours()) { }

    private static AnyEmployeeId AnyValidEmployee() {
        return new AnyEmployeeId();
    }

    private static AnyEnum<ContractType> AnyValidType() {
        return Any.Enum<ContractType>();
    }

    private static AnyDateOnly AnyValidHiredOn() {
        return Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, new DateOnly(2090, 12, 31));
    }

    private static AnyInt32 AnyValidLengthInDays() {
        return Any.Int32().Between(1, 3_650);
    }

    private static AnyHourlyRate AnyValidRate() {
        return new AnyHourlyRate();
    }

    private static AnyInt32 AnyValidWeeklyHours() {
        // 35 is the ceiling of every contract type, so the default is valid whatever the type.
        return Any.Int32().Between(1, 35);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEmploymentContract(AnyEmployeeId         employee,
                                  AnyEnum<ContractType> type,
                                  AnyDateOnly           hiredOn,
                                  AnyInt32              lengthInDays,
                                  AnyHourlyRate         rate,
                                  AnyInt32              weeklyHours) {
        _employee          = employee;
        _employeeRecipe    = employee;
        _type              = type;
        _typeRecipe        = type;
        _hiredOn           = hiredOn;
        _hiredOnRecipe     = hiredOn;
        _lengthInDays       = lengthInDays;
        _lengthInDaysRecipe = lengthInDays;
        _rate              = rate;
        _rateRecipe        = rate;
        _weeklyHours       = weeklyHours;
        _weeklyHoursRecipe = weeklyHours;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEmploymentContract(AnyEmploymentContract from) {
        _employee          = from._employee;
        _employeeRecipe    = from._employeeRecipe;
        _type              = from._type;
        _typeRecipe        = from._typeRecipe;
        _hiredOn           = from._hiredOn;
        _hiredOnRecipe     = from._hiredOnRecipe;
        _endsOn             = from._endsOn;
        _lengthInDays       = from._lengthInDays;
        _lengthInDaysRecipe = from._lengthInDaysRecipe;
        _rate              = from._rate;
        _rateRecipe        = from._rateRecipe;
        _weeklyHours       = from._weeklyHours;
        _weeklyHoursRecipe = from._weeklyHoursRecipe;
    }

    /// <summary>Pins <c>employee</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithEmployee(EmployeeId value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._employee = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>employee</c> is drawn from — <c>AnyValidEmployee()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEmployeeId, AnyEmployeeId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentContract WithEmployee(Func<AnyEmployeeId, AnyEmployeeId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._employeeRecipe = configure(_employeeRecipe);
        copy._employee       = copy._employeeRecipe;

        return copy;
    }

    /// <summary>Pins <c>type</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithType(ContractType value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._type = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>type</c> is drawn from — <c>AnyValidType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ContractType&gt;, AnyEnum&lt;ContractType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentContract WithType(Func<AnyEnum<ContractType>, AnyEnum<ContractType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._typeRecipe = configure(_typeRecipe);
        copy._type       = copy._typeRecipe;

        return copy;
    }

    /// <summary>Pins <c>hiredOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithHiredOn(DateOnly value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._hiredOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hiredOn</c> is drawn from — <c>AnyValidHiredOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentContract WithHiredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._hiredOnRecipe = configure(_hiredOnRecipe);
        copy._hiredOn       = copy._hiredOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>endsOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithEndsOn(DateOnly? value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._endsOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>endsOn</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithoutEndsOn() {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._endsOn = Any.Fixed<DateOnly?>(null);

        return copy;
    }

    /// <summary>Pins <c>rate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithRate(HourlyRate value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rate</c> is drawn from — <c>AnyValidRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyHourlyRate, AnyHourlyRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentContract WithRate(Func<AnyHourlyRate, AnyHourlyRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    /// <summary>Pins <c>weeklyHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentContract WithWeeklyHours(int value) {
        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._weeklyHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>weeklyHours</c> is drawn from — <c>AnyValidWeeklyHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentContract WithWeeklyHours(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentContract copy = new AnyEmploymentContract(this);

        copy._weeklyHoursRecipe = configure(_weeklyHoursRecipe);
        copy._weeklyHours       = copy._weeklyHoursRecipe;

        return copy;
    }

    /// <summary>
    ///     Produces one arbitrary <see cref="EmploymentContract" />. A contract that is not permanent ends
    ///     <c>lengthInDays</c> after it starts, unless <c>WithEndsOn</c> / <c>WithoutEndsOn</c> pinned the end.
    /// </summary>
    public EmploymentContract Generate() {
        ContractType type    = _type.Generate();
        DateOnly     hiredOn = _hiredOn.Generate();
        DateOnly?    endsOn  = _endsOn is not null ? _endsOn.Generate()
                             : type == ContractType.Permanent ? null
                             : hiredOn.AddDays(_lengthInDays.Generate());

        return new EmploymentContract(_employee.Generate(),
                                      type,
                                      hiredOn,
                                      endsOn,
                                      _rate.Generate(),
                                      _weeklyHours.Generate());
    }

}
