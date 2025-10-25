using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class VacationRecordConfigurations : IEntityTypeConfiguration<VacationRecord>
{
    public void Configure(EntityTypeBuilder<VacationRecord> builder)
    {
        builder.ToTable("vacation_records");

        builder.Property(vr => vr.ManagerComment).HasMaxLength(250);
        builder.Ignore(vr => vr.DaysCount);
        
        builder.Property(vr=> vr.StartDate)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v)         // Convert to DateOnly when reading
            )
            .HasColumnType("date"); // Use 'date' instead of 'datetime' in SQL
        
        builder.Property(vr=> vr.EndDate)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v)
            )
            .HasColumnType("date");

        builder.HasOne(vr => vr.Employee)
            .WithMany(e => e.VacationRecords)
            .HasForeignKey(vr => vr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}