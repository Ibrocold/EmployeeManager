using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Dtos;

public record UpdateAssignmentRequest(DateTime AssignmentDate, AssignmentStatus Status);