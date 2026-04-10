using MortgagePro.Domain.Entities;

namespace MortgagePro.Application.Services;

public class ReactiveMortgageEngine
{
    private List<ScheduleRow> _schedule = new();
    private readonly object _computeLock = new object();

    public IReadOnlyList<ScheduleRow> GetSchedule()
    {
        lock (_computeLock)
        {
            return _schedule.ToList();
        }
    }

    public void Hydrate(IEnumerable<ScheduleRow> state)
    {
        lock (_computeLock)
        {
            _schedule = state.ToList();
        }
    }

    public void ChangeGlobalStrategy(bool shortenTerm)
    {
        lock (_computeLock)
        {
            for (int i = 0; i < _schedule.Count; i++)
            {
                _schedule[i] = _schedule[i] with { IsStrategyShortenTerm = shortenTerm };
            }
            if (_schedule.Count > 0)
            {
                RecalculateSequential(0);
            }
        }
    }

    public void GenerateInitial(decimal amount, int months, decimal initialRate, bool isStrategyShortenTerm, decimal initialOverpayment, bool isDecreasingInstallment, bool isSnowballEffect = false, decimal? targetMaxInstallment = null)
    {
        lock (_computeLock)
        {
            _schedule.Clear();
            decimal remainingBalance = amount;
            decimal fixedPrincipalConstant = isDecreasingInstallment ? (amount / months) : 0;
            
            decimal theoreticalBalance = amount;

            for (int i = 1; i <= months; i++)
            {
                if (remainingBalance <= 0) break;

                decimal monthlyRate = initialRate / 100m / 12m;
                decimal baseInstallment = 0;
                decimal principalPortion = 0;
                int remainingMonths = months - i + 1; 

                decimal theoMonthlyRate = initialRate / 100m / 12m;
                decimal theoreticalInstallment = 0;
                if (theoMonthlyRate > 0)
                     theoreticalInstallment = theoreticalBalance * theoMonthlyRate * (decimal)Math.Pow(1 + (double)theoMonthlyRate, remainingMonths) / (decimal)(Math.Pow(1 + (double)theoMonthlyRate, remainingMonths) - 1);
                else
                     theoreticalInstallment = theoreticalBalance / remainingMonths;

                if (isDecreasingInstallment)
                {
                    if (isStrategyShortenTerm) {
                        principalPortion = fixedPrincipalConstant;
                    } else {
                        principalPortion = remainingBalance / remainingMonths;
                    }
                    if (principalPortion > remainingBalance) principalPortion = remainingBalance;
                    decimal interestPortion = remainingBalance * monthlyRate;
                    baseInstallment = principalPortion + interestPortion;
                    
                    theoreticalBalance -= fixedPrincipalConstant;
                }
                else
                {
                    if (isStrategyShortenTerm)
                    {
                        baseInstallment = theoreticalInstallment;
                    }
                    else
                    {
                        if (monthlyRate > 0)
                            baseInstallment = remainingBalance * monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, remainingMonths) / (decimal)(Math.Pow(1 + (double)monthlyRate, remainingMonths) - 1);
                        else
                            baseInstallment = remainingBalance / remainingMonths;
                    }

                    decimal interestPortion = remainingBalance * monthlyRate;
                    principalPortion = baseInstallment - interestPortion;
                    
                    decimal theoInterest = theoreticalBalance * monthlyRate;
                    theoreticalBalance -= (theoreticalInstallment - theoInterest);
                }

                if (principalPortion > remainingBalance)
                {
                    principalPortion = remainingBalance;
                    baseInstallment = principalPortion + (remainingBalance * monthlyRate);
                }

                decimal interestActual = remainingBalance * monthlyRate;
                
                // Dynamic Overpayment Logic
                decimal computedOverpayment = initialOverpayment;
                if (targetMaxInstallment.HasValue)
                {
                    decimal diff = targetMaxInstallment.Value - baseInstallment;
                    computedOverpayment = diff > 0 ? diff : 0;
                }
                else if (isSnowballEffect)
                {
                    decimal diff = theoreticalInstallment - baseInstallment;
                    if (diff > 0) computedOverpayment += diff;
                }

                decimal nextRemainingBalance = remainingBalance - principalPortion - computedOverpayment;
                if (nextRemainingBalance < 0) nextRemainingBalance = 0;

                _schedule.Add(new ScheduleRow(
                    MonthId: i,
                    RemainingBalance: nextRemainingBalance,
                    BaseInstallment: baseInstallment,
                    PrincipalPortion: principalPortion,
                    InterestPortion: interestActual,
                    Overpayment: initialOverpayment,
                    InterestRate: initialRate,
                    IsStrategyShortenTerm: isStrategyShortenTerm,
                    IsDecreasingInstallment: isDecreasingInstallment,
                    FixedPrincipalConstant: fixedPrincipalConstant,
                    OriginalBaseInstallment: theoreticalInstallment,
                    OriginalTermMonths: months,
                    IsSnowballEffect: isSnowballEffect,
                    TargetMaxInstallment: targetMaxInstallment
                ));

                remainingBalance = nextRemainingBalance;
            }
        }
    }

    public void ApplyCascadeUpdate(int startMonthId, decimal? newOverpayment = null, decimal? newInterestRate = null)
    {
        lock (_computeLock)
        {
            var updateIndex = _schedule.FindIndex(r => r.MonthId == startMonthId);
            if (updateIndex == -1) return;

            for (int i = updateIndex; i < _schedule.Count; i++)
            {
                var row = _schedule[i];
                _schedule[i] = row with 
                { 
                    Overpayment = newOverpayment ?? row.Overpayment,
                    InterestRate = newInterestRate ?? row.InterestRate
                };
            }
            RecalculateSequential(updateIndex);
        }
    }

    public void ApplySingleUpdate(int monthId, decimal? newOverpayment = null, decimal? newInterestRate = null)
    {
        lock (_computeLock)
        {
            var updateIndex = _schedule.FindIndex(r => r.MonthId == monthId);
            if (updateIndex == -1) return;

            var row = _schedule[updateIndex];
            _schedule[updateIndex] = row with 
            { 
                Overpayment = newOverpayment ?? row.Overpayment,
                InterestRate = newInterestRate ?? row.InterestRate
            };
            
            RecalculateSequential(updateIndex);
        }
    }

    private void RecalculateSequential(int startIndex)
    {
        for (int i = startIndex; i < _schedule.Count; i++)
        {
            var currentRow = _schedule[i];
            
            if (i > 0 && _schedule[i - 1].RemainingBalance <= 0)
            {
                 _schedule.RemoveRange(i, _schedule.Count - i);
                 break;
            }

            decimal newRemainingBalance;
            if (i == 0)
            {
                // Edge case dla odświeżania roota
                newRemainingBalance = currentRow.RemainingBalance + currentRow.PrincipalPortion + currentRow.ComputedOverpayment;
            }
            else
            {
                newRemainingBalance = _schedule[i - 1].RemainingBalance;
            }

            if (newRemainingBalance < 0) newRemainingBalance = 0;

            decimal newInterestPortion = newRemainingBalance * (currentRow.InterestRate / 100m / 12m);
            decimal newBaseInstallment = currentRow.BaseInstallment;
            decimal newPrincipalPortion = currentRow.PrincipalPortion;
            
            decimal monthlyRate = currentRow.InterestRate / 100m / 12m;
            int remainingMonths = currentRow.OriginalTermMonths - i;

            if (currentRow.IsDecreasingInstallment)
            {
                if (currentRow.IsStrategyShortenTerm)
                {
                    newPrincipalPortion = currentRow.FixedPrincipalConstant;
                    if (newPrincipalPortion > newRemainingBalance) newPrincipalPortion = newRemainingBalance;
                }
                else
                {
                    if (remainingMonths > 0)
                         newPrincipalPortion = newRemainingBalance / remainingMonths;
                    else
                         newPrincipalPortion = newRemainingBalance;
                         
                    _schedule[i] = _schedule[i] with { FixedPrincipalConstant = newPrincipalPortion };
                }
                newBaseInstallment = newPrincipalPortion + newInterestPortion;
            }
            else 
            {
                if (currentRow.IsStrategyShortenTerm)
                {
                   newBaseInstallment = currentRow.OriginalBaseInstallment;
                   newPrincipalPortion = newBaseInstallment - newInterestPortion;
                   if (newPrincipalPortion > newRemainingBalance)
                   {
                       newPrincipalPortion = newRemainingBalance;
                   }
                }
                else 
                {
                   if (remainingMonths > 0)
                   {
                       if (monthlyRate > 0)
                           newBaseInstallment = newRemainingBalance * monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, remainingMonths) / (decimal)(Math.Pow(1 + (double)monthlyRate, remainingMonths) - 1);
                       else
                           newBaseInstallment = newRemainingBalance / remainingMonths;
                   }
                   else
                   {
                       newBaseInstallment = newRemainingBalance + newInterestPortion;
                   }
                   newPrincipalPortion = newBaseInstallment - newInterestPortion;
                }
            }
            
            // Dynamic Overpayment computation for re-amortization
            decimal actualComputedOverpayment = currentRow.Overpayment;
            if (currentRow.TargetMaxInstallment.HasValue)
            {
                decimal diff = currentRow.TargetMaxInstallment.Value - newBaseInstallment;
                actualComputedOverpayment = diff > 0 ? diff : 0;
            }
            else if (currentRow.IsSnowballEffect)
            {
                decimal diff = currentRow.OriginalBaseInstallment - newBaseInstallment;
                if (diff > 0) actualComputedOverpayment += diff;
            }

            decimal nextRemainingBalance = newRemainingBalance - newPrincipalPortion - actualComputedOverpayment;
            if (nextRemainingBalance < 0) nextRemainingBalance = 0;

            _schedule[i] = _schedule[i] with 
            {
                RemainingBalance = nextRemainingBalance,
                InterestPortion = newInterestPortion,
                PrincipalPortion = newPrincipalPortion,
                BaseInstallment = newBaseInstallment
            };
            
            if (i == _schedule.Count - 1 && nextRemainingBalance > 0.05m && i < 1500)
            {
                _schedule.Add(_schedule[i] with 
                {
                    MonthId = _schedule[i].MonthId + 1,
                    RemainingBalance = nextRemainingBalance,
                    Overpayment = 0
                });
            }
        }
    }
}
