using MortgagePro.Application.Services;
using MortgagePro.Domain.Entities;

namespace MortgagePro.Tests;

/// <summary>
/// Testy jednostkowe silnika rekalkulacyjnego.
/// Weryfikują poprawność algorytmów rat równych, malejących,
/// strategii nadpłat oraz cascade updates.
/// </summary>
public class ReactiveMortgageEngineTests
{
    private readonly ReactiveMortgageEngine _engine;

    public ReactiveMortgageEngineTests()
    {
        _engine = new ReactiveMortgageEngine();
    }

    // ================================
    // TESTY GENEROWANIA HARMONOGRAMU
    // ================================

    [Fact]
    public void GenerateInitial_EqualInstallments_ReturnsCorrectMonthCount()
    {
        // Arrange & Act
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        // Assert
        Assert.Equal(120, schedule.Count);
    }

    [Fact]
    public void GenerateInitial_DecreasingInstallments_ReturnsCorrectMonthCount()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: true);
        var schedule = _engine.GetSchedule();

        Assert.Equal(120, schedule.Count);
    }

    [Fact]
    public void GenerateInitial_EqualInstallments_FirstInstallmentIsPositive()
    {
        _engine.GenerateInitial(amount: 500000, months: 360, initialRate: 7.2m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        Assert.True(schedule[0].BaseInstallment > 0);
        Assert.True(schedule[0].PrincipalPortion > 0);
        Assert.True(schedule[0].InterestPortion > 0);
    }

    [Fact]
    public void GenerateInitial_EqualInstallments_LastMonthBalanceIsZero()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        // Saldo na koniec powinno być równe 0 (lub bardzo bliskie 0 z powodu zaokrągleń)
        Assert.True(schedule.Last().RemainingBalance < 1m,
            $"Ostatnie saldo: {schedule.Last().RemainingBalance} powinno być ~0");
    }

    [Fact]
    public void GenerateInitial_DecreasingInstallments_LastMonthBalanceIsZero()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: true);
        var schedule = _engine.GetSchedule();

        Assert.True(schedule.Last().RemainingBalance < 1m,
            $"Ostatnie saldo: {schedule.Last().RemainingBalance} powinno być ~0");
    }

    [Fact]
    public void GenerateInitial_EqualInstallments_AllInstallmentsAreSame()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        // Przy braku nadpłat wszystkie raty bazowe powinny być identyczne
        var firstInstallment = schedule[0].BaseInstallment;
        foreach (var row in schedule)
        {
            Assert.True(Math.Abs(row.BaseInstallment - firstInstallment) < 0.01m,
                $"Miesiąc {row.MonthId}: rata {row.BaseInstallment} != {firstInstallment}");
        }
    }

    [Fact]
    public void GenerateInitial_DecreasingInstallments_InstallmentsDecrease()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: true);
        var schedule = _engine.GetSchedule();

        // Przy ratach malejących, pierwsza rata powinna być wyższa od ostatniej
        Assert.True(schedule.First().BaseInstallment > schedule.Last().BaseInstallment);
    }

    // ================================
    // TESTY SUMY SPŁACONEGO KAPITAŁU
    // ================================

    [Fact]
    public void GenerateInitial_EqualInstallments_TotalPrincipalEqualsLoanAmount()
    {
        decimal amount = 200000;
        _engine.GenerateInitial(amount: amount, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        decimal totalPrincipal = schedule.Sum(r => r.PrincipalPortion);
        Assert.True(Math.Abs(totalPrincipal - amount) < 1m,
            $"Suma kapitału {totalPrincipal} powinna wynosić {amount}");
    }

    [Fact]
    public void GenerateInitial_DecreasingInstallments_TotalPrincipalEqualsLoanAmount()
    {
        decimal amount = 200000;
        _engine.GenerateInitial(amount: amount, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: true);
        var schedule = _engine.GetSchedule();

        decimal totalPrincipal = schedule.Sum(r => r.PrincipalPortion);
        Assert.True(Math.Abs(totalPrincipal - amount) < 1m,
            $"Suma kapitału {totalPrincipal} powinna wynosić {amount}");
    }

    // ================================
    // TESTY NADPŁAT I STRATEGII
    // ================================

    [Fact]
    public void GenerateInitial_WithOverpayment_ShortenTerm_ReducesMonthCount()
    {
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 100, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        // Z nadpłatą 100 zł/msc przy strategii "Skracaj Okres" powinno być mniej niż 240 rat
        Assert.True(schedule.Count < 240,
            $"Ilość rat {schedule.Count} powinna być < 240 przy nadpłacie i skracaniu okresu");
    }

    [Fact]
    public void GenerateInitial_WithOverpayment_ReduceInstallment_KeepsMonthCount()
    {
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: false, initialOverpayment: 100, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        // Przy strategii "Zmniejszaj Ratę" okres powinien się utrzymać (lub nieznacznie skrócić)
        Assert.True(schedule.Count >= 230,
            $"Ilość rat {schedule.Count} powinna być zbliżona do 240 przy zmniejszaniu raty");
    }

    [Fact]
    public void ShortenTerm_SavesMoreInterest_ThanReduceInstallment()
    {
        // Strategia "Skracaj Okres"
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 100, isDecreasingInstallment: false);
        decimal interestShorten = _engine.GetSchedule().Sum(r => r.InterestPortion);

        // Strategia "Zmniejszaj Ratę"
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: false, initialOverpayment: 100, isDecreasingInstallment: false);
        decimal interestReduce = _engine.GetSchedule().Sum(r => r.InterestPortion);

        Assert.True(interestShorten < interestReduce,
            $"Skracanie ({interestShorten:F0}) powinno dać mniej odsetek niż zmniejszanie raty ({interestReduce:F0})");
    }

    [Fact]
    public void DecreasingInstallments_PayLessInterest_ThanEqualInstallments()
    {
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: true);
        decimal interestDecreasing = _engine.GetSchedule().Sum(r => r.InterestPortion);

        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        decimal interestEqual = _engine.GetSchedule().Sum(r => r.InterestPortion);

        Assert.True(interestDecreasing < interestEqual,
            $"Malejące ({interestDecreasing:F0}) powinny mieć mniej odsetek niż równe ({interestEqual:F0})");
    }

    // ================================
    // TESTY CASCADE UPDATE
    // ================================

    [Fact]
    public void CascadeUpdate_Overpayment_AppliesFromGivenMonth()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);

        _engine.ApplyCascadeUpdate(startMonthId: 10, newOverpayment: 500, newInterestRate: null);
        var schedule = _engine.GetSchedule();

        // Miesiące 1-9 powinny mieć nadpłatę 0
        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(0m, schedule[i].Overpayment);
        }

        // Miesiąc 10 i dalej powinny mieć nadpłatę 500
        Assert.Equal(500m, schedule[9].Overpayment);
    }

    [Fact]
    public void CascadeUpdate_InterestRate_ChangesFromGivenMonth()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);

        _engine.ApplyCascadeUpdate(startMonthId: 24, newOverpayment: null, newInterestRate: 3.0m);
        var schedule = _engine.GetSchedule();

        // Przed miesiącem 24 oprocentowanie 5%
        Assert.Equal(5.0m, schedule[0].InterestRate);

        // Od miesiąca 24 oprocentowanie 3%
        Assert.Equal(3.0m, schedule[23].InterestRate);
    }

    // ================================
    // TESTY ZMIANY STRATEGII
    // ================================

    [Fact]
    public void ChangeGlobalStrategy_RecalculatesSchedule()
    {
        _engine.GenerateInitial(amount: 500000, months: 240, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 200, isDecreasingInstallment: false);
        var scheduleShorten = _engine.GetSchedule().ToList();

        _engine.ChangeGlobalStrategy(shortenTerm: false);
        var scheduleReduce = _engine.GetSchedule().ToList();

        // Po zmianie strategii harmonogramy powinny się różnić
        Assert.NotEqual(scheduleShorten.Count, scheduleReduce.Count);
    }

    // ================================
    // TESTY HYDRATE / SNAPSHOT
    // ================================

    [Fact]
    public void Hydrate_RestoresExactState()
    {
        _engine.GenerateInitial(amount: 100000, months: 60, initialRate: 4.0m,
            isStrategyShortenTerm: true, initialOverpayment: 50, isDecreasingInstallment: false);
        var originalSchedule = _engine.GetSchedule().ToList();

        // Generujemy coś zupełnie innego
        _engine.GenerateInitial(amount: 999999, months: 12, initialRate: 10.0m,
            isStrategyShortenTerm: false, initialOverpayment: 0, isDecreasingInstallment: true);

        // Hydrate powraca do oryginału
        _engine.Hydrate(originalSchedule);
        var restoredSchedule = _engine.GetSchedule();

        Assert.Equal(originalSchedule.Count, restoredSchedule.Count);
        for (int i = 0; i < originalSchedule.Count; i++)
        {
            Assert.Equal(originalSchedule[i].MonthId, restoredSchedule[i].MonthId);
            Assert.Equal(originalSchedule[i].RemainingBalance, restoredSchedule[i].RemainingBalance);
        }
    }

    // ================================
    // TESTY EDGE CASES
    // ================================

    [Fact]
    public void GenerateInitial_ZeroInterestRate_DoesNotThrow()
    {
        _engine.GenerateInitial(amount: 100000, months: 120, initialRate: 0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        Assert.Equal(120, schedule.Count);
        Assert.True(schedule.All(r => r.InterestPortion == 0),
            "Przy 0% oprocentowania odsetki powinny być 0");
    }

    [Fact]
    public void GenerateInitial_VeryHighOverpayment_ShortensScheduleSignificantly()
    {
        _engine.GenerateInitial(amount: 100000, months: 360, initialRate: 5.0m,
            isStrategyShortenTerm: true, initialOverpayment: 5000, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        Assert.True(schedule.Count < 30,
            $"Przy nadpłacie 5000 zł/msc kredyt 100k powinien skończyć się szybko, a trwa {schedule.Count} msc");
    }

    [Fact]
    public void GenerateInitial_AllBalancesAreNonNegative()
    {
        _engine.GenerateInitial(amount: 300000, months: 360, initialRate: 7.0m,
            isStrategyShortenTerm: true, initialOverpayment: 500, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        Assert.True(schedule.All(r => r.RemainingBalance >= 0),
            "Żadne saldo nie powinno być ujemne");
    }

    [Fact]
    public void GenerateInitial_SmallLoan_ShortTerm_WorksCorrectly()
    {
        _engine.GenerateInitial(amount: 10000, months: 12, initialRate: 3.0m,
            isStrategyShortenTerm: true, initialOverpayment: 0, isDecreasingInstallment: false);
        var schedule = _engine.GetSchedule();

        Assert.Equal(12, schedule.Count);
        Assert.True(schedule.Last().RemainingBalance < 1m);
    }
}
