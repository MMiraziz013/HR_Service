using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data;

public class DataContext : IdentityDbContext<User, IdentityRole<int>, int>, IDataContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }
    
    public async Task MigrateAsync()    
    {
        await Database.MigrateAsync();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        //TODO: Add assembly configurations

    } 
}