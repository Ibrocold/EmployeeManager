using EmployeeManager.Application.Dtos;
using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.API.Controllers
{
    /// <summary>
    /// Checks run in the fixed order published in the Assignment 02 brief:
    ///   1. Model binding and data annotations - [ApiController], 400
    ///   2. Does the addressed assignment exist? (GET/PUT/DELETE) - 404
    ///   3. Do EmployeeId/DepartmentId resolve? (POST) - 400
    ///   4. BR-02 - AssignmentDate range - 400
    ///   5. BR-05 - not the employee's permanent department (POST) - 400
    ///   6. BR-04 - status transition state machine (PUT) - 400
    ///   7. BR-01 - conflicting Active assignment (PUT) - 409
    /// </summary>
    [Route("api/assignment")]
    [ApiController]
    public class EmployeeDepartmentAssignmentController : ControllerBase
    {
        private readonly ILogger<EmployeeDepartmentAssignmentController> _logger;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeDepartmentAssignmentRepository _assignmentRepository;

        public EmployeeDepartmentAssignmentController(
            ILogger<EmployeeDepartmentAssignmentController> logger,
            IEmployeeRepository employeeRepository,
            IEmployeeDepartmentAssignmentRepository assignmentRepository)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
            _assignmentRepository = assignmentRepository;
        }

        /// <summary>GET /api/assignment/{id} - 200 when found, 404 when not.</summary>
        [HttpGet]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetAssignmentById(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching assignment with id {id}", id);

            var assignment = await _assignmentRepository.GetAssignmentById(id, cancellationToken);

            if (assignment is null) return NotFound();

            return Ok(ToResponse(assignment));
        }

        /// <summary>
        /// POST /api/assignment - 201 Created with a Location header, or 400 on invalid input.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateAssignment(
            [FromBody] CreateAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            // Step 3: EmployeeId resolves
            var employee = await _employeeRepository.GetEmployeeById(request.EmployeeId, cancellationToken);
            if (employee is null)
            {
                ModelState.AddModelError(
                    nameof(request.EmployeeId),
                    $"Employee {request.EmployeeId} does not exist.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Step 3: DepartmentId resolves
            if (!await _employeeRepository.DepartmentExists(request.DepartmentId, cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(request.DepartmentId),
                    $"Department {request.DepartmentId} does not exist.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Step 4: BR-02 - AssignmentDate must not be more than 31 days in the future
            if (!IsDateWithinRange(request.AssignmentDate))
            {
                ModelState.AddModelError(
                    nameof(request.AssignmentDate),
                    "AssignmentDate must not be more than 31 days in the future.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Step 5: BR-05 - cannot assign to the employee's own permanent department
            if (employee.DepartmentId == request.DepartmentId)
            {
                ModelState.AddModelError(
                    nameof(request.DepartmentId),
                    "Employee cannot be temporarily assigned to their own permanent department.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // BR-03: always created as Scheduled, regardless of anything the client sent
            var created = await _assignmentRepository.CreateAssignment(
                new EmployeeDepartmentAssignment
                {
                    EmployeeId = request.EmployeeId,
                    DepartmentId = request.DepartmentId,
                    AssignmentDate = request.AssignmentDate,
                    Status = AssignmentStatus.Scheduled
                },
                cancellationToken);

            _logger.LogInformation("Created assignment with id {id}", created.AssignmentId);

            return CreatedAtAction(nameof(GetAssignmentById), new { id = created.AssignmentId }, ToResponse(created));
        }

        /// <summary>
        /// PUT /api/assignment/{id} - 200 with the updated resource, 404 if it does not
        /// exist, 400 on an invalid date or illegal transition, 409 on an active-assignment conflict.
        /// </summary>
        [HttpPut]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateAssignment(
            int id,
            [FromBody] UpdateAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            // Step 2: does the resource exist? A 404 outranks every 400/409 below.
            var existing = await _assignmentRepository.GetAssignmentById(id, cancellationToken);

            if (existing is null) return NotFound();

            // Step 4: BR-02, only re-checked when the date is actually changing
            if (request.AssignmentDate != existing.AssignmentDate && !IsDateWithinRange(request.AssignmentDate))
            {
                ModelState.AddModelError(
                    nameof(request.AssignmentDate),
                    "AssignmentDate must not be more than 31 days in the future.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Step 6: BR-04, evaluated before BR-01 so an illegal transition is always 400
            if (!IsTransitionAllowed(existing.Status, request.Status))
            {
                ModelState.AddModelError(
                    nameof(request.Status),
                    $"Cannot transition from {existing.Status} to {request.Status}.");

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Step 7: BR-01, only relevant when transitioning into Active
            if (request.Status == AssignmentStatus.Active && existing.Status != AssignmentStatus.Active)
            {
                var hasActive = await _assignmentRepository.HasActiveAssignment(
                    existing.EmployeeId, existing.AssignmentId, cancellationToken);

                if (hasActive)
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflicting active assignment",
                        Detail = $"Employee {existing.EmployeeId} already has a different active assignment."
                    });
                }
            }

            var updated = await _assignmentRepository.UpdateAssignment(
                id,
                new EmployeeDepartmentAssignment
                {
                    AssignmentDate = request.AssignmentDate,
                    Status = request.Status
                },
                cancellationToken);

            // Still possible if the row was deleted between the two calls above.
            if (updated is null) return NotFound();

            _logger.LogInformation("Updated assignment with id {id}", id);

            return Ok(ToResponse(updated));
        }

        /// <summary>DELETE /api/assignment/{id} - 204 on success, 404 if it does not exist.</summary>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAssignmentById(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting assignment with id {id}", id);

            var isDeleted = await _assignmentRepository.DeleteAssignmentIfExist(id, cancellationToken);

            if (!isDeleted) return NotFound();

            return NoContent();
        }

        private static bool IsDateWithinRange(DateTime assignmentDate) =>
            assignmentDate.Date <= DateTime.UtcNow.Date.AddDays(31);

        private static readonly Dictionary<AssignmentStatus, AssignmentStatus[]> AllowedTransitions = new()
        {
            [AssignmentStatus.Scheduled] = new[] { AssignmentStatus.Active, AssignmentStatus.Cancelled },
            [AssignmentStatus.Active] = new[] { AssignmentStatus.Completed, AssignmentStatus.Cancelled },
            [AssignmentStatus.Completed] = Array.Empty<AssignmentStatus>(),
            [AssignmentStatus.Cancelled] = Array.Empty<AssignmentStatus>()
        };

        private static bool IsTransitionAllowed(AssignmentStatus from, AssignmentStatus to) =>
            from == to || AllowedTransitions[from].Contains(to);

        //Mapping entity -> DTO in one place keeps the controller actions readable.
        private static AssignmentResponse ToResponse(EmployeeDepartmentAssignment assignment) =>
            new(assignment.AssignmentId, assignment.EmployeeId, assignment.DepartmentId, assignment.AssignmentDate, assignment.Status);
    }
}