using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class VacationBalanceConfigurations : IEntityTypeConfiguration<VacationBalance>
{
    public void Configure(EntityTypeBuilder<VacationBalance> builder)
    {
        builder.ToTable("vacation_balances");

        builder.Ignore(vb => vb.RemainingDays);
    }
}