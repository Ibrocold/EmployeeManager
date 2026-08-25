namespace EmployeeManagerApi.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Xunit;
using EmployeeManager.Application.Dtos;
using EmployeeManager.Core.Models;
using EmployeeManagerApi.IntegrationTests.Urls;

[Collection(IntegrationTestCollection.Name)]
public class EmployeeDepartmentAssignmentControllerTests
{
    private readonly HttpClient _client;
    private readonly ApiTestFixture _fixture;

    public EmployeeDepartmentAssignmentControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact] // BR-05
    public async Task Create_SameAsPermanentDepartment_Returns400()
    {
        int permanentDeptId = 0;
        _fixture.WithDbContext(db =>
        {
            permanentDeptId = db.Employees.Single(e => e.Id == 1).DepartmentId;
        });

        var dto = new { employeeId = 1, departmentId = permanentDeptId, assignmentDate = DateTime.UtcNow };
        var resp = await _client.PostAsJsonAsync(ApiRoutes.Assignments.Base, dto);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact] // BR-02
    public async Task Create_DateTooFarInFuture_Returns400()
    {
        var dto = new { employeeId = 1, departmentId = 6, assignmentDate = DateTime.UtcNow.Date.AddDays(32) };
        var resp = await _client.PostAsJsonAsync(ApiRoutes.Assignments.Base, dto);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact] // BR-03
    public async Task Create_IgnoresSuppliedStatus_AlwaysScheduled()
    {
        var dto = new { employeeId = 2, departmentId = 6, assignmentDate = DateTime.UtcNow, status = "Active" };
        var resp = await _client.PostAsJsonAsync(ApiRoutes.Assignments.Base, dto);
        var body = await resp.Content.ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal(AssignmentStatus.Scheduled, body!.Status);
    }

    [Fact] // BR-04
    public async Task Update_IllegalTransition_Returns400()
    {
        var createDto = new { employeeId = 3, departmentId = 6, assignmentDate = DateTime.UtcNow };
        var createResp = await _client.PostAsJsonAsync(ApiRoutes.Assignments.Base, createDto);
        var created = await createResp.Content.ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        var updateDto = new { assignmentDate = created!.AssignmentDate, status = "Completed" }; // Scheduled -> Completed illegal
        var resp = await _client.PutAsJsonAsync(ApiRoutes.Assignments.ById(created.AssignmentId), updateDto);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact] // BR-01
    public async Task Update_ToActive_WhenAlreadyActiveElsewhere_Returns409()
    {
        const int employeeId = 4;
        int targetId = 0;

        _fixture.WithDbContext(db =>
        {
            db.Add(new EmployeeDepartmentAssignment
            {
                EmployeeId = employeeId,
                DepartmentId = 6,
                AssignmentDate = DateTime.UtcNow,
                Status = AssignmentStatus.Active
            });

            var scheduled = new EmployeeDepartmentAssignment
            {
                EmployeeId = employeeId,
                DepartmentId = 7,
                AssignmentDate = DateTime.UtcNow,
                Status = AssignmentStatus.Scheduled
            };
            db.Add(scheduled);

            db.SaveChanges();
            targetId = scheduled.AssignmentId;
        });

        var updateDto = new { assignmentDate = DateTime.UtcNow, status = "Active" };
        var resp = await _client.PutAsJsonAsync(ApiRoutes.Assignments.ById(targetId), updateDto);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact] // precedence: 404 beats BR-02
    public async Task Update_NonExistentId_Returns404_EvenWithBadDate()
    {
        var dto = new { assignmentDate = DateTime.UtcNow.AddDays(90), status = "Active" };
        var resp = await _client.PutAsJsonAsync(ApiRoutes.Assignments.ById(999999), dto);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}