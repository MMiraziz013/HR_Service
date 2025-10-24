using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Clean.Application.Abstractions;

public interface IDataContext
{
    public DbSet<User> Users { get; set; }
    //TODO: Add the rest of the DbSets
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task MigrateAsync();
    
    DatabaseFacade Database { get; }
}