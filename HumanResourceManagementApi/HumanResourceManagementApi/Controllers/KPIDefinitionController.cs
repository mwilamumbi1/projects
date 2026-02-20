using Core.HumanResourceManagementApi.Models;

using Core.HumanResourceManagementApi.DTOs;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;

using Microsoft.EntityFrameworkCore;

using System.Linq;

using System.Threading.Tasks;



namespace Core.HumanResourceManagementApi.Controllers

{

    [ApiController]

    [Route("api/[controller]")]

    public class KPIDefinitionController : ControllerBase

    {

        private readonly HRDataContext _context;



        public KPIDefinitionController(HRDataContext context)

        {

            _context = context;

        }

        [HttpGet("GetAllKPI")]

        public async Task<IActionResult> GetAllKPI()

        {

            try

            {

                var result = await _context.Set<GetAllKpiResult>()

                  .FromSqlRaw("EXEC [HR].[GetAllKPIDefinitions]")

                  .ToListAsync();



                return Ok(result);

            }

            catch (SqlException ex)

            {

                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });

            }

        }



        // ---------------- ADD KPI ----------------

        [HttpPost("AddKPI")]

        public async Task<IActionResult> AddKPI([FromBody] AddKPIDefinitionDto dto)

        {

            var parameters = new[]

            {

        new SqlParameter("@KPIName", string.IsNullOrWhiteSpace(dto.KPIName) ? (object)DBNull.Value : dto.KPIName.Trim()),

        new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description?.Trim()),



        new SqlParameter("@IsActive", dto.IsActive.HasValue ? dto.IsActive : (object)DBNull.Value)

      };



            try

            {

                var result = await _context.Set<AddKPIDefinitionResult>()

                  .FromSqlRaw("EXEC [HR].[AddKPIDefinition] @KPIName, @Description,  @IsActive", parameters)

                  .ToListAsync();



                var spResult = result.FirstOrDefault();

                if (spResult == null)

                    return StatusCode(500, new { error = "No result returned from stored procedure." });



                if (!spResult.Success)

                    return BadRequest(new { error = spResult.Message });



                return Ok(new { message = spResult.Message });

            }

            catch (SqlException ex)

            {

                return BadRequest(new { error = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });

            }

        }



        // ---------------- GET KPI BY ID ----------------

        [HttpGet("GetKpiDefinitionById/{id}")]

        public async Task<IActionResult> GetKpiDefinitionById(int id)

        {

            if (id <= 0)

                return BadRequest(new { error = "KPI_ID must be a positive integer." });



            var parameter = new SqlParameter("@KPI_ID", id);



            try

            {

                var result = await _context.Set<GetKpiDefinitionByIdResult>()

                  .FromSqlRaw("EXEC HR.GetKPIDefinitionByID @KPI_ID", parameter)

                  .ToListAsync();



                var spResult = result.FirstOrDefault();

                if (spResult == null)

                    return NotFound(new { error = "KPI not found." });



                return Ok(spResult);

            }

            catch (SqlException ex)

            {

                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });

            }

        }



        // ---------------- UPDATE KPI ----------------

        [HttpPut("UpdateKPI")]

        public async Task<IActionResult> UpdateKPI([FromBody] UpdateKPIDefinitionDto dto)

        {

            var parameters = new[]

            {

        new SqlParameter("@KPI_ID", dto.KPI_ID),

        new SqlParameter("@KPI_Name", dto.KPI_Name ?? (object)DBNull.Value),

        new SqlParameter("@Description", dto.Description ?? (object)DBNull.Value),

        new SqlParameter("@IsActive", dto.IsActive.HasValue ? dto.IsActive : (object)DBNull.Value)

      };



            try

            {

                var result = await _context.Set<UpdateKPIDefinitionResult>()

                  .FromSqlRaw("EXEC [HR].[UpdateKPIDefinition] @KPI_ID, @KPI_Name, @Description, @IsActive", parameters)

                  .ToListAsync();



                var spResult = result.FirstOrDefault();

                if (spResult == null)

                    return StatusCode(500, new { error = "No result returned from stored procedure." });



                if (!spResult.Success)

                    return BadRequest(new { error = spResult.Message });



                return Ok(new { message = spResult.Message });

            }

            catch (SqlException ex)

            {

                return BadRequest(new { error = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });

            }

        }



        // ---------------- TOGGLE STATUS ----------------

        [HttpPut("ToggleStatus/{id}")]

        public async Task<IActionResult> ToggleStatus(int id)

        {

            var parameter = new SqlParameter("@KPI_ID", id);



            try

            {

                var result = await _context.Set<ToggleKPIStatusResult>()

                  .FromSqlRaw("EXEC [HR].[ToggleKPIStatus] @KPI_ID", parameter)

                  .ToListAsync();



                var spResult = result.FirstOrDefault();

                if (spResult == null)

                    return NotFound(new { error = "KPI not found or toggle failed." });



                return Ok(new { message = spResult.Message });

            }

            catch (SqlException ex)

            {

                return BadRequest(new { error = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });

            }

        }



        // ---------------- DELETE KPI ----------------

        [HttpDelete("DeleteKPI/{id}")]

        public async Task<IActionResult> DeleteKPI(int id)

        {

            var parameter = new SqlParameter("@KPI_ID", id);



            try

            {

                var result = await _context.Set<DeleteKPIDefinitionResult>()

                  .FromSqlRaw("EXEC [HR].[DeleteKPIDefinition] @KPI_ID", parameter)

                  .ToListAsync();



                var spResult = result.FirstOrDefault();

                if (spResult == null)

                    return NotFound(new { error = "KPI not found or delete failed." });



                return Ok(new { message = spResult.Message });

            }

            catch (SqlException ex)

            {

                return BadRequest(new { error = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });

            }

        }

    }



    // ---------------- DTOs & Results ----------------

    public class GetKpiDefinitionByIdDto

    {

        public int KPI_ID { get; set; }

    }



    public class GetKpiDefinitionByIdResult

    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? KPI_ID { get; set; }

        public string? KPI_Name { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

    }



    public class AddKPIDefinitionDto

    {

        public string KPIName { get; set; } = string.Empty;

        public string? Description { get; set; }



        public bool? IsActive { get; set; }

    }



    public class AddKPIDefinitionResult

    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

    }



    public class UpdateKPIDefinitionDto

    {

        public int KPI_ID { get; set; }

        public string? KPI_Name { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

    }



    public class UpdateKPIDefinitionResult

    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

    }



    public class ToggleKPIStatusResult

    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

    }



    public class DeleteKPIDefinitionResult

    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

    }







    [Keyless]

    public class GetAllKpiResult

    {

        public int KPI_ID { get; set; }

        public string KPI_Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

    }



}