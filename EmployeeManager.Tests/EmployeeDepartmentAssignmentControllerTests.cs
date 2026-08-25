using EmployeeManager.API.Controllers;
using EmployeeManager.Application.Dtos;
using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeManager.Tests;

public class EmployeeDepartmentAssignmentControllerTests
{
    private readonly Mock<ILogger<EmployeeDepartmentAssignmentController>> _logger = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IEmployeeDepartmentAssignmentRepository> _assignmentRepo = new();
    private readonly EmployeeDepartmentAssignmentController _controller;

    public EmployeeDepartmentAssignmentControllerTests()
    {
        _controller = new EmployeeDepartmentAssignmentController(_logger.Object, _employeeRepo.Object, _assignmentRepo.Object);
    }

    [Fact]
    public async Task Create_UnknownEmployeeId_Returns400()
    {
        _employeeRepo.Setup(r => r.GetEmployeeById(999, It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

        var result = await _controller.CreateAssignment(new CreateAssignmentRequest(999, 1, DateTime.UtcNow), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Create_SameAsPermanentDepartment_Returns400()
    {
        var employee = new Employee { Id = 1, DepartmentId = 3 };
        _employeeRepo.Setup(r => r.GetEmployeeById(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _employeeRepo.Setup(r => r.DepartmentExists(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.CreateAssignment(new CreateAssignmentRequest(1, 3, DateTime.UtcNow), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        _assignmentRepo.Setup(r => r.GetAssignmentById(999, It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeDepartmentAssignment?)null);

        var result = await _controller.UpdateAssignment(999, new UpdateAssignmentRequest(DateTime.UtcNow, AssignmentStatus.Active), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_IllegalTransition_Returns400()
    {
        var existing = new EmployeeDepartmentAssignment { AssignmentId = 1, EmployeeId = 1, Status = AssignmentStatus.Completed, AssignmentDate = DateTime.UtcNow };
        _assignmentRepo.Setup(r => r.GetAssignmentById(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _controller.UpdateAssignment(1, new UpdateAssignmentRequest(DateTime.UtcNow, AssignmentStatus.Active), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Update_ToActive_WhenAlreadyActiveElsewhere_Returns409()
    {
        var existing = new EmployeeDepartmentAssignment { AssignmentId = 2, EmployeeId = 5, Status = AssignmentStatus.Scheduled, AssignmentDate = DateTime.UtcNow };
        _assignmentRepo.Setup(r => r.GetAssignmentById(2, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _assignmentRepo.Setup(r => r.HasActiveAssignment(5, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.UpdateAssignment(2, new UpdateAssignmentRequest(DateTime.UtcNow, AssignmentStatus.Active), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, problem.Status);
    }
}