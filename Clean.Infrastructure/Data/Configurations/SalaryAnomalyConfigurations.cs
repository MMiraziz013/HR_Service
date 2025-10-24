using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class SalaryAnomalyConfigurations : IEntityTypeConfiguration<SalaryAnomaly>
{
    public void Configure(EntityTypeBuilder<SalaryAnomaly> builder)
    {
        builder.ToTable("salary_anomalies");

        builder.Property(sa => sa.ReviewComment).HasMaxLength(250);
        
        builder.Property(sa=> sa.Month)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v) // Convert to DateOnly when reading
            );

        builder.HasOne(sa => sa.Employee)
            .WithMany(e => e.SalaryAnomalies)
            .HasForeignKey(sa => sa.EmployeeId);
    }
}