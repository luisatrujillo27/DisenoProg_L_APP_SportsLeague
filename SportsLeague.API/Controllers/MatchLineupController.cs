using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers
{
    [ApiController]
    [Route("api/match/{matchId}")]
    public class MatchLineupController : ControllerBase
    {
        private readonly IMatchLineupService _matchLineupService;
        private readonly IMapper _mapper;

        public MatchLineupController(IMatchLineupService matchLineupService, IMapper mapper)
        {
            _matchLineupService = matchLineupService;
            _mapper = mapper;
        }

        // POST /api/match/{matchId}/lineup  → 201
        [HttpPost("lineup")]
        public async Task<ActionResult<MatchLineupDto>> AddToLineup(int matchId, CreateMatchLineupDto dto)
        {
            try
            {
                var lineup = _mapper.Map<MatchLineup>(dto);
                var created = await _matchLineupService.AddToLineupAsync(matchId, lineup);

                // Re-consultar con Include para poblar PlayerName y TeamName
                var fullLineup = await _matchLineupService.GetLineupByMatchAsync(matchId);
                var detailed = fullLineup.First(x => x.Id == created.Id);
                var responseDto = _mapper.Map<MatchLineupDto>(detailed);

                return CreatedAtAction(nameof(GetLineup), new { matchId }, responseDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // GET /api/match/{matchId}/lineup  → 200
        [HttpGet("lineup")]
        public async Task<ActionResult<IEnumerable<MatchLineupDto>>> GetLineup(int matchId)
        {
            try
            {
                var lineup = await _matchLineupService.GetLineupByMatchAsync(matchId);
                return Ok(_mapper.Map<IEnumerable<MatchLineupDto>>(lineup));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET /api/match/{matchId}/lineup/team/{teamId}  → 200
        [HttpGet("lineup/team/{teamId}")]
        public async Task<ActionResult<IEnumerable<MatchLineupDto>>> GetLineupByTeam(int matchId, int teamId)
        {
            try
            {
                var lineup = await _matchLineupService.GetLineupByTeamAsync(matchId, teamId);
                return Ok(_mapper.Map<IEnumerable<MatchLineupDto>>(lineup));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE /api/match/{matchId}/lineup/{id}  → 204
        [HttpDelete("lineup/{id}")]
        public async Task<ActionResult> RemoveFromLineup(int matchId, int id)
        {
            try
            {
                await _matchLineupService.RemoveFromLineupAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
