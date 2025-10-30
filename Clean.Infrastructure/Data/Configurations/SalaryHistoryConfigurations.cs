using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class SalaryHistoryConfigurations : IEntityTypeConfiguration<SalaryHistory>
{
    public void Configure(EntityTypeBuilder<SalaryHistory> builder)
    {
        builder.ToTable("salary_histories");

        builder.Ignore(sh => sh.ExpectedTotal);
        
        builder.Property(sh => sh.BaseAmount).HasColumnType("decimal(18,2)");
        builder.Property(sh => sh.BonusAmount).HasColumnType("decimal(18,2)");
        
        builder.Property(sh => sh.Month)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v) // Convert to DateOnly when reading
                )
            .HasColumnType("date"); 

        builder.HasOne(sh => sh.Employee)
            .WithMany(e => e.SalaryHistories)
            .HasForeignKey(sh => sh.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}