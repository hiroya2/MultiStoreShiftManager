using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeShiftRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeShiftRequestsController(AppDbContext context)
    {
        _context = context;
    }

    // 優先度の入力ルールを確認
    private bool IsPriorityValid(string requestType, int? priority)
    {
        if (requestType == "PreferOff")
        {
            return priority.HasValue &&
                   priority.Value >= 1 &&
                   priority.Value <= 3;
        }

        return priority == null;
    }

    // 日付単位の希望情報を登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeShiftRequestCreateRequest request)
    {
        if (!IsPriorityValid(request.RequestType, request.Priority))
        {
            return BadRequest(
                "希望休の場合は優先度1～3が必要です。出勤不可・出勤可能の場合は優先度を設定できません。");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == request.EmployeeId &&
                e.IsActive &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return BadRequest(
                "指定された従業員が存在しないか、無効です。");
        }

        var exists = await _context.EmployeeShiftRequests
            .AnyAsync(r =>
                r.EmployeeId == request.EmployeeId &&
                r.RequestDate == request.RequestDate);

        if (exists)
        {
            return Conflict(
                "同じ従業員・希望対象日の希望情報が既に登録されています。");
        }

        var shiftRequest = new EmployeeShiftRequest
        {
            EmployeeId = request.EmployeeId,
            RequestDate = request.RequestDate,
            RequestType = request.RequestType,
            Priority = request.Priority,
            Comment = request.Comment
        };

        _context.EmployeeShiftRequests.Add(shiftRequest);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = shiftRequest.Id },
            shiftRequest);
    }

    // 希望情報を1件取得
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shiftRequest = await _context.EmployeeShiftRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (shiftRequest == null)
        {
            return NotFound("希望情報が見つかりません。");
        }

        return Ok(shiftRequest);
    }

    // 希望情報を一覧取得
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] DateOnly? requestDate)
    {
        var query = _context.EmployeeShiftRequests
            .Include(r => r.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(r =>
                r.EmployeeId == employeeId.Value);
        }

        if (requestDate.HasValue)
        {
            query = query.Where(r =>
                r.RequestDate == requestDate.Value);
        }

        var requests = await query
            .OrderBy(r => r.RequestDate)
            .ThenBy(r => r.EmployeeId)
            .ToListAsync();

        return Ok(requests);
    }

    // 希望情報を更新
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeShiftRequestUpdateRequest request)
    {
        if (!IsPriorityValid(request.RequestType, request.Priority))
        {
            return BadRequest(
                "希望休の場合は優先度1～3が必要です。出勤不可・出勤可能の場合は優先度を設定できません。");
        }

        var shiftRequest = await _context.EmployeeShiftRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (shiftRequest == null)
        {
            return NotFound("希望情報が見つかりません。");
        }

        shiftRequest.RequestType = request.RequestType;
        shiftRequest.Priority = request.Priority;
        shiftRequest.Comment = request.Comment;
        shiftRequest.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(shiftRequest);
    }
}