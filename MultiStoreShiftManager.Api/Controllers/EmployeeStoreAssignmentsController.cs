using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeStoreAssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeStoreAssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            EmployeeStoreAssignmentCreateRequest request)
        {
            // 従業員が存在し、有効か確認
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.Id == request.EmployeeId &&
                    e.IsActive &&
                    e.EmploymentStatus == "Active");

            if (employee == null)
            {
                return BadRequest("指定された従業員が存在しないか、無効です。");
            }

            // 店舗が存在し、有効か確認
            var store = await _context.Stores
                .FirstOrDefaultAsync(s =>
                    s.Id == request.StoreId &&
                    s.IsActive);

            if (store == null)
            {
                return BadRequest("指定された店舗が存在しないか、無効です。");
            }

            // 終了日が開始日より前でないか確認
            if (request.EndDate.HasValue &&
                request.EndDate.Value < request.StartDate)
            {
                return BadRequest("勤務可能終了日は開始日より後の日付にしてください。");
            }

            // 同じ従業員・店舗で期間が重複していないか確認
            var hasOverlap = await _context.EmployeeStoreAssignments
                .AnyAsync(a =>
                    a.EmployeeId == request.EmployeeId &&
                    a.StoreId == request.StoreId &&
                    (!a.EndDate.HasValue || a.EndDate.Value >= request.StartDate) &&
                    (!request.EndDate.HasValue || a.StartDate <= request.EndDate.Value));

            if (hasOverlap)
            {
                return Conflict("同じ従業員・店舗の勤務可能期間が重複しています。");
            }

            // 登録データを作成
            var assignment = new EmployeeStoreAssignment
            {
                EmployeeId = request.EmployeeId,
                StoreId = request.StoreId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.EmployeeStoreAssignments.Add(assignment);

            await _context.SaveChangesAsync();

            return StatusCode(201, assignment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            int? employeeId = null,
            int? storeId = null,
            bool includeEnded = false)
        {
            var query = _context.EmployeeStoreAssignments
                .AsNoTracking()
                .AsQueryable();

            // 従業員IDで絞り込み
            if (employeeId.HasValue)
            {
                query = query.Where(a =>
                    a.EmployeeId == employeeId.Value);
            }

            // 店舗IDで絞り込み
            if (storeId.HasValue)
            {
                query = query.Where(a =>
                    a.StoreId == storeId.Value);
            }

            // 終了済みの割り当てを除外
            if (!includeEnded)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                query = query.Where(a =>
                    !a.EndDate.HasValue ||
                    a.EndDate.Value >= today);
            }

            var assignments = await query
                .OrderBy(a => a.EmployeeId)
                .ThenBy(a => a.StoreId)
                .ThenBy(a => a.StartDate)
                .Select(a => new
                {
                    a.Id,
                    a.EmployeeId,
                    EmployeeName = a.Employee.EmployeeName,
                    a.StoreId,
                    StoreName = a.Store.StoreName,
                    a.StartDate,
                    a.EndDate,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Ok(assignments);

        }
        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndAssignment(
            int id,
            DateOnly endDate)
        {
            var assignment = await _context.EmployeeStoreAssignments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound("勤務可能店舗の割り当てが見つかりません。");
            }

            if (assignment.EndDate.HasValue)
            {
                return BadRequest("この割り当ては既に終了しています。");
            }

            if (endDate < assignment.StartDate)
            {
                return BadRequest("終了日は開始日より後の日付にしてください。");
            }

            assignment.EndDate = endDate;
            assignment.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(assignment);
        }
    }
}