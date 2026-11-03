using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // 従業員一覧取得
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] bool includeInactive = false)
    {
        var query = _context.Employees.AsQueryable();

        // 無効な従業員を含めない場合
        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        var employees = await query
            .OrderBy(e => e.Id)
            .ToListAsync();

        return Ok(employees);
    }

    // 従業員登録
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        EmployeeCreateRequest request)
    {
        // 同じ従業員管理番号が登録済みか確認
        var exists = await _context.Employees
            .AnyAsync(e => e.EmployeeCode == request.EmployeeCode);

        if (exists)
        {
            return Conflict(new
            {
                message = "この従業員管理番号は既に登録されています。"
            });
        }

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            EmployeeName = request.EmployeeName,
            EmploymentType = request.EmploymentType,
            EmploymentStatus = "Active",
            HireDate = request.HireDate,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(CreateEmployee),
            new { id = employee.Id },
            employee);
    }

    // 従業員情報更新
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        EmployeeUpdateRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "指定された従業員が見つかりません。"
            });
        }

        // 同じ管理番号を持つ別の従業員がいないか確認
        var exists = await _context.Employees
            .AnyAsync(e =>
                e.EmployeeCode == request.EmployeeCode &&
                e.Id != id);

        if (exists)
        {
            return Conflict(new
            {
                message = "この従業員管理番号は既に登録されています。"
            });
        }

        employee.EmployeeCode = request.EmployeeCode;
        employee.EmployeeName = request.EmployeeName;
        employee.EmploymentType = request.EmploymentType;
        employee.HireDate = request.HireDate;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(employee);
    }

    // 従業員の論理削除
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "指定された従業員が見つかりません。"
            });
        }

        if (!employee.IsActive)
        {
            return BadRequest(new
            {
                message = "この従業員は既に無効になっています。"
            });
        }

        employee.IsActive = false;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "従業員を無効化しました。",
            employee
        });
    }

}