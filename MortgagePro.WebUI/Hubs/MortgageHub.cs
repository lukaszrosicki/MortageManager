using Microsoft.AspNetCore.SignalR;
using MortgagePro.Application.Services;
using MortgagePro.Domain.Entities;

namespace MortgagePro.WebUI.Hubs;

public class MortgageHub : Hub
{
    private readonly ReactiveMortgageEngine _engine;

    public MortgageHub(ReactiveMortgageEngine engine)
    {
        _engine = engine;
    }

    public async Task InitializeMortgage(decimal amount, int months, decimal rate, bool shortenTerm, decimal overpayment, bool isDecreasing, bool isSnowball, decimal? maxInstallment)
    {
        _engine.GenerateInitial(amount, months, rate, shortenTerm, overpayment, isDecreasing, isSnowball, maxInstallment);
        await Clients.All.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }

    public async Task CascadeUpdate(int startMonthId, decimal? newOverpayment, decimal? newInterestRate)
    {
        _engine.ApplyCascadeUpdate(startMonthId, newOverpayment, newInterestRate);
        await Clients.All.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }

    public async Task SingleUpdate(int monthId, decimal? newOverpayment, decimal? newInterestRate)
    {
        _engine.ApplySingleUpdate(monthId, newOverpayment, newInterestRate);
        await Clients.All.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }

    public async Task ChangeGlobalStrategy(bool shortenTerm)
    {
        _engine.ChangeGlobalStrategy(shortenTerm);
        await Clients.All.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }

    public async Task GetCurrentSchedule()
    {
        await Clients.Caller.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }

    public async Task HydrateScenario(IEnumerable<ScheduleRow> snapshot)
    {
        _engine.Hydrate(snapshot);
        await Clients.All.SendAsync("ScheduleUpdated", _engine.GetSchedule());
    }
}
