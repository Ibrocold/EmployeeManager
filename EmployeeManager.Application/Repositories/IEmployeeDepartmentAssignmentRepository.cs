using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Repositories;

public interface IEmployeeDepartmentAssignmentRepository
{
    Task<EmployeeDepartmentAssignment?> GetAssignmentById(int id, CancellationToken cancellationToken = default);

    Task<EmployeeDepartmentAssignment> CreateAssignment(EmployeeDepartmentAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the supplied AssignmentDate and Status to the assignment with the given id.
    /// EmployeeId and DepartmentId on the passed-in entity are ignored — they are immutable once set.
    /// Returns null when no assignment with that id exists.
    /// </summary>
    Task<EmployeeDepartmentAssignment?> UpdateAssignment(int id, EmployeeDepartmentAssignment assignment, CancellationToken cancellationToken = default);

    Task<bool> DeleteAssignmentIfExist(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the employee already has a different assignment with Status = Active.
    /// Used to enforce BR-01 before a PUT transitions an assignment to Active, so the
    /// conflict is caught here instead of surfacing as a database constraint failure.
    /// </summary>
    Task<bool> HasActiveAssignment(int employeeId, int excludingAssignmentId, CancellationToken cancellationToken = default);
}