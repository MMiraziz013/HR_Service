using Clean.Application.Abstractions;
using Clean.Application.Dtos.SalaryAnomaly;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/salary_anomaly")]
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
      return StatusCode(response.StatusCode, response);
   }

   [HttpGet("get-unviewed")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> GetUnviewed()
   {
      var response = await _salaryAnomaly.GetUnviewedAsync();
      return StatusCode(response.StatusCode, response);
   }

   [HttpGet("get")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.View)]
   public async Task<IActionResult> GetByEmployeeId([FromQuery] int employeeId)
   {
      var response = await _salaryAnomaly.GetAnomalyByEmployeeId(employeeId);
      return StatusCode(response.StatusCode, response);
   }
   [HttpPut("mark-viewed")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> MarkAsViewed([FromBody] int id)
   {
      var response = await _salaryAnomaly.MarkAsViewedAsync(id);
      return StatusCode(response.StatusCode, response);
   }

   [HttpPut("add-comment")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> AddReviewComment([FromBody] AddReviewCommnetDto dto)
   {
      var response = await _salaryAnomaly.AddReviewCommentAsync(dto.Id, dto.ReviewComment);
      return StatusCode(response.StatusCode, response);
   }

   [HttpGet("get-list")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> GetSalaryAnomaliesAsList()
   {
      var response = await _salaryAnomaly.GetSalaryAnomaliesForListAsync();
      return StatusCode(response.StatusCode, response);
   }

   [HttpDelete("delete")]
   [PermissionAuthorize(PermissionConstants.SalaryAnomalies.Manage)]
   public async Task<IActionResult> Delete([FromBody] int id)
   {
      var response = await _salaryAnomaly.DeleteAsync(id);
      return StatusCode(response.StatusCode, response);
   }


}