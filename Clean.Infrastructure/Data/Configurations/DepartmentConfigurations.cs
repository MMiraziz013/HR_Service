using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class DepartmentConfigurations : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(300).IsRequired();

        builder.HasMany(d => d.Employees)
            .WithOne(e => e.Department);
    }
}