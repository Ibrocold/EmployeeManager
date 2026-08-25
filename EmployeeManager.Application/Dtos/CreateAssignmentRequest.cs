namespace EmployeeManager.Application.Dtos;

public record CreateAssignmentRequest(int EmployeeId, int DepartmentId, DateTime AssignmentDate);