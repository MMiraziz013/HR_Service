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
        
        builder.Property(vb => vb.PeriodStart)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v)         // Convert to DateOnly when reading
            )
            .HasColumnType("date"); // Use 'date' instead of 'datetime' in SQL
        
        builder.Property(vb=> vb.PeriodEnd)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v)         // Convert to DateOnly when reading
            )
            .HasColumnType("date"); // Use 'date' instead of 'datetime' in SQL
        
        
        builder.HasIndex(vb => vb.EmployeeId);
        builder.HasIndex(vb => vb.Year);
    }
}