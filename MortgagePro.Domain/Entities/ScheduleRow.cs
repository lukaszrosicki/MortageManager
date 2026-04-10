namespace MortgagePro.Domain.Entities;

public record ScheduleRow(
    int MonthId,
    decimal RemainingBalance,
    decimal BaseInstallment,
    decimal PrincipalPortion,
    decimal InterestPortion,
    decimal Overpayment,
    decimal InterestRate,  
    bool IsStrategyShortenTerm,
    bool IsDecreasingInstallment,
    decimal FixedPrincipalConstant,
    decimal OriginalBaseInstallment,
    int OriginalTermMonths,
    bool IsSnowballEffect,
    decimal? TargetMaxInstallment
)
{
    public decimal ComputedOverpayment 
    {
        get
        {
            decimal over = Overpayment;
            if (TargetMaxInstallment.HasValue)
            {
                decimal diff = TargetMaxInstallment.Value - BaseInstallment;
                over = diff > 0 ? diff : 0;
            }
            else if (IsSnowballEffect)
            {
                decimal diff = OriginalBaseInstallment - BaseInstallment;
                if (diff > 0)
                {
                    over += diff; // Dodanie różnicy z malejącej raty
                }
            }
            return over;
        }
    }

    public decimal ActualPaid => PrincipalPortion + InterestPortion + ComputedOverpayment;
}
