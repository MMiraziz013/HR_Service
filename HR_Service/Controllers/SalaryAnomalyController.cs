using Clean.Application.Abstractions;
using Clean.Application.Dtos.SalaryAnomaly;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalaryAnomalyController : Controller
{
   private readonly ISalaryAnomalyService _salaryAnomaly;

   public SalaryAnomalyController(ISalaryAnomalyService salaryAnomaly)
   {
      _salaryAnomaly = salaryAnomaly;
   }

   [HttpGet("get-all")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> GetAllAsync()
   {
      var response = await _salaryAnomaly.GetAllAsync();
      return Ok(response);
   }

   [HttpGet("get-unviewed")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> GetUnviewed()
   {
      var response = await _salaryAnomaly.GetUnviewedAsync();
      return Ok(response);
   }

   [HttpGet("get")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.View)]
   public async Task<IActionResult> GetByEmployeeId([FromQuery] int employeeId)
   {
      var response = await _salaryAnomaly.GetAnomalyByEmployeeId(employeeId);
      return Ok(response);
   }
   [HttpPut("mark-viewed")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> MarkAsViewed([FromBody] int id)
   {
      var response = await _salaryAnomaly.MarkAsViewedAsync(id);
      return Ok(response);
   }

   [HttpPut("add-comment")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> AddReviewComment([FromBody] AddReviewCommnetDto dto)
   {
      var response = await _salaryAnomaly.AddReviewCommentAsync(dto.Id, dto.ReviewComment);
      return Ok(response);
   }

   [HttpDelete("delete")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> Delete([FromBody] int id)
   {
      var response = await _salaryAnomaly.DeleteAsync(id);
      return Ok(response);
   }


}