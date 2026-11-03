using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>Written by hand: one arbitrary income of any of the three kinds.</summary>
public sealed class AnyIncomeComponent : IAny<IncomeComponent> {

    private readonly IncomeKind _kind;

    public AnyIncomeComponent()
        : this(IncomeKind.All) { }

    private AnyIncomeComponent(IncomeKind kind) {
        _kind = kind;
    }

    public AnyIncomeComponent OfKind(IncomeKind kind) {
        if (kind == IncomeKind.None) { throw new ArgumentException("An income has a kind.", nameof(kind)); }

        return new AnyIncomeComponent(kind);
    }

    public IncomeComponent Generate() {
        IAny<IncomeComponent>[] candidates = Candidates();

        return Any.Int32().Between(0, candidates.Length - 1).Then(index => candidates[index]).Generate();
    }

    private IAny<IncomeComponent>[] Candidates() {
        System.Collections.Generic.List<IAny<IncomeComponent>> candidates = [];
        if ((_kind & IncomeKind.Employment) != 0) { candidates.Add(new AnyEmploymentIncome().As(i => (IncomeComponent)i)); }
        if ((_kind & IncomeKind.SelfEmployment) != 0) { candidates.Add(new AnySelfEmploymentIncome().As(i => (IncomeComponent)i)); }
        if ((_kind & IncomeKind.Investment) != 0) { candidates.Add(new AnyInvestmentIncome().As(i => (IncomeComponent)i)); }

        return candidates.ToArray();
    }

}
