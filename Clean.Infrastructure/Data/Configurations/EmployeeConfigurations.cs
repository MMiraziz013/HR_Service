using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class EmployeeConfigurations : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(50).IsRequired();
        // builder.Property(e => e.BaseSalary).HasColumnType("decimal(18,2)");

        
        builder.Property(e => e.HireDate)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v)         // Convert to DateOnly when reading
            )
            .HasColumnType("date"); // Use 'date' instead of 'datetime' in SQL

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.SalaryHistories)
            .WithOne(s => s.Employee);
        
        builder.HasMany(e => e.VacationRecords)
            .WithOne(sr => sr.Employee);

        builder.HasMany(e => e.SalaryAnomalies)
            .WithOne(sa => sa.Employee);
        
        builder.HasMany(e => e.VacationBalances)
            .WithOne(vb => vb.Employee)
            .OnDelete(DeleteBehavior.Cascade);
        
    }
}