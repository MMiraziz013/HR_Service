using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Seed;

public class SeedDepartments : IDataSeeder
{
    private readonly DataContext _context;

    public SeedDepartments(DataContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (await _context.Departments.AnyAsync())
            return; // already seeded

        var departments = new List<Department>
        {
            new Department { Name = "Engineering", Description = "Handles software development and system architecture" },
            new Department { Name = "Human Resources", Description = "Manages hiring and employee well-being" },
            new Department { Name = "Finance", Description = "Responsible for budgeting and payroll" }
        };

        _context.Departments.AddRange(departments);
        await _context.SaveChangesAsync();
    }
}
