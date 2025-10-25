using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clean.Application.Abstractions;

public interface IEmployeeRepository
{
    //TODO: Finish Employee Repository methods
    Task<bool> AddAsync(Employee employee);
}