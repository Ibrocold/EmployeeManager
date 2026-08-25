using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure;

public class EmployeeDepartmentAssignmentRepository : IEmployeeDepartmentAssignmentRepository
{
    private readonly AppDbContext _context;

    public EmployeeDepartmentAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<EmployeeDepartmentAssignment?> GetAssignmentById(int id, CancellationToken cancellationToken = default) =>
        _context.EmployeeDepartmentAssignments.FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

    public async Task<EmployeeDepartmentAssignment> CreateAssignment(EmployeeDepartmentAssignment assignment, CancellationToken cancellationToken = default)
    {
        _context.EmployeeDepartmentAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<EmployeeDepartmentAssignment?> UpdateAssignment(int id, EmployeeDepartmentAssignment assignment, CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (existing is null)
            return null;

        existing.AssignmentDate = assignment.AssignmentDate;
        existing.Status = assignment.Status;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAssignmentIfExist(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (existing is null)
            return false;

        _context.EmployeeDepartmentAssignments.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> HasActiveAssignment(int employeeId, int excludingAssignmentId, CancellationToken cancellationToken = default) =>
        _context.EmployeeDepartmentAssignments.AnyAsync(a =>
            a.EmployeeId == employeeId &&
            a.AssignmentId != excludingAssignmentId &&
            a.Status == AssignmentStatus.Active,
            cancellationToken);
}