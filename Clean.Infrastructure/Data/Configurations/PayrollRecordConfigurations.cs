using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class PayrollRecordConfigurations :IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.ToTable("payroll_records");

        builder.Ignore(pr => pr.NetPay);
        
        builder.Property(pr => pr.GrossPay).HasColumnType("decimal(18,2)");
        builder.Property(pr => pr.Deductions).HasColumnType("decimal(18,2)");

        builder.Property(pr => pr.CreatedAt).HasDefaultValueSql("NOW()");
        
        builder.Property(pr=> pr.PeriodStart)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v) // Convert to DateOnly when reading
            );
         builder.Property(pr=> pr.PeriodEnd)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v)
            );

         builder.HasOne(pr => pr.Employee)
             .WithMany(e => e.PayrollRecords)
             .HasForeignKey(pr => pr.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);
    }
}