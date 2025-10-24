using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Infrastructure.Data.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.UserName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.RegistrationDate).HasDefaultValueSql("NOW()");
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<Employee>(e => e.UserId);
    }
}