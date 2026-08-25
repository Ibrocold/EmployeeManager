using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Dtos;

public record AssignmentResponse(
    int AssignmentId,
    int EmployeeId,
    int DepartmentId,
    DateTime AssignmentDate,
    AssignmentStatus Status);