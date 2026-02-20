using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Core.HumanResourceManagementApi.Models;

namespace Core.HumanResourceManagementApi.Models
{
    [Keyless]
    public class UpdateReviewObjectiveDto
    {
        [Required]
        public int ObjectiveID { get; set; }

        public string ObjectiveDescription { get; set; }

        [Range(0, 999.99)]
        public decimal Score { get; set; }

        [Range(0, 999.99)]
        public decimal Weight { get; set; }

        public string Comments { get; set; }
    }
}

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdateReviewObjectiveController : ControllerBase
    {
        private readonly HRDataContext _context;

        public UpdateReviewObjectiveController(HRDataContext context)
        {
            _context = context;
        }

        [HttpPut("{objectiveId}")]
        public async Task<IActionResult> UpdateReviewObjective(int objectiveId, [FromBody] UpdateReviewObjectiveDto updateObjectiveDto)
        {
            if (objectiveId <= 0)
            {
                return BadRequest("ObjectiveID must be a positive integer.");
            }

            if (updateObjectiveDto == null)
            {
                return BadRequest("Update objective data is required.");
            }

            // Ensure the route parameter matches the DTO
            updateObjectiveDto.ObjectiveID = objectiveId;

            var parameters = new[]
            {
                new SqlParameter("@ObjectiveID", updateObjectiveDto.ObjectiveID),
                new SqlParameter("@ObjectiveDescription", updateObjectiveDto.ObjectiveDescription ?? (object)DBNull.Value),
                new SqlParameter("@Score", updateObjectiveDto.Score),
                new SqlParameter("@Weight", updateObjectiveDto.Weight),
                new SqlParameter("@Comments", updateObjectiveDto.Comments ?? (object)DBNull.Value)
            };

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.UpdateReviewObjective @ObjectiveID, @ObjectiveDescription, @Score, @Weight, @Comments",
                    parameters);

                return Ok(new { Message = "Review objective updated successfully" });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = "Failed to update review objective", Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReviewObjectiveByBody([FromBody] UpdateReviewObjectiveDto updateObjectiveDto)
        {
            if (updateObjectiveDto == null || updateObjectiveDto.ObjectiveID <= 0)
            {
                return BadRequest("Valid objective data with ObjectiveID is required.");
            }

            var parameters = new[]
            {
                new SqlParameter("@ObjectiveID", updateObjectiveDto.ObjectiveID),
                new SqlParameter("@ObjectiveDescription", updateObjectiveDto.ObjectiveDescription ?? (object)DBNull.Value),
                new SqlParameter("@Score", updateObjectiveDto.Score),
                new SqlParameter("@Weight", updateObjectiveDto.Weight),
                new SqlParameter("@Comments", updateObjectiveDto.Comments ?? (object)DBNull.Value)
            };

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.UpdateReviewObjective @ObjectiveID, @ObjectiveDescription, @Score, @Weight, @Comments",
                    parameters);

                return Ok(new { Message = "Review objective updated successfully" });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = "Failed to update review objective", Error = ex.Message });
            }
        }
    }
}