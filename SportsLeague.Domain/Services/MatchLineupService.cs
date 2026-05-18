using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Helpers;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services
{
    public class MatchLineupService : IMatchLineupService
    {
        private readonly IMatchLineupRepository _lineupRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly MatchValidationHelper _matchValidationHelper;
        private readonly ILogger<MatchLineupService> _logger;

        public MatchLineupService(
            IMatchLineupRepository lineupRepository,
            IMatchRepository matchRepository,
            MatchValidationHelper matchValidationHelper,
            ILogger<MatchLineupService> logger)
        {
            _lineupRepository = lineupRepository;
            _matchRepository = matchRepository;
            _matchValidationHelper = matchValidationHelper;
            _logger = logger;
        }

        public async Task<MatchLineup> AddToLineupAsync(int matchId, MatchLineup lineup)
        {
            // V1 — El partido debe existir
            var match = await _matchRepository.GetByIdAsync(matchId);
            if (match == null)
                throw new KeyNotFoundException(
                    $"No se encontró el partido con ID {matchId}");

            // V6 — El partido debe estar en estado Scheduled
            if (match.Status != MatchStatus.Scheduled)
                throw new InvalidOperationException(
                    "Solo se pueden registrar alineaciones en partidos Scheduled");

            // V2 + V3 — Jugador existe y pertenece al Home o Away
            var player = await _matchValidationHelper
                .ValidatePlayerInMatchAsync(lineup.PlayerId, match);

            // V4 — El jugador no puede estar dos veces en la misma alineación
            if (await _lineupRepository.ExistsByMatchAndPlayerAsync(matchId, lineup.PlayerId))
                throw new InvalidOperationException(
                    "El jugador ya está registrado en la alineación de este partido");

            // V5 — Máximo 11 titulares por equipo (los suplentes NO cuentan)
            if (lineup.IsStarter)
            {
                var teamLineup = await _lineupRepository
                    .GetByMatchAndTeamAsync(matchId, player.TeamId);
                if (teamLineup.Count(x => x.IsStarter) >= 11)
                    throw new InvalidOperationException(
                        "El equipo ya tiene 11 titulares registrados en este partido");
            }

            lineup.MatchId = matchId;

            _logger.LogInformation(
                "Adding player {PlayerId} to lineup of match {MatchId} (Starter={IsStarter})",
                lineup.PlayerId, matchId, lineup.IsStarter);
            return await _lineupRepository.CreateAsync(lineup);
        }

        public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId)
        {
            var match = await _matchRepository.GetByIdAsync(matchId);
            if (match == null)
                throw new KeyNotFoundException(
                    $"No se encontró el partido con ID {matchId}");

            return await _lineupRepository.GetByMatchAsync(matchId);
        }

        public async Task<IEnumerable<MatchLineup>> GetLineupByTeamAsync(int matchId, int teamId)
        {
            var match = await _matchRepository.GetByIdAsync(matchId);
            if (match == null)
                throw new KeyNotFoundException(
                    $"No se encontró el partido con ID {matchId}");

            return await _lineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
        }

        public async Task RemoveFromLineupAsync(int id)
        {
            if (!await _lineupRepository.ExistsAsync(id))
                throw new KeyNotFoundException(
                    $"No se encontró el registro de alineación con ID {id}");

            await _lineupRepository.DeleteAsync(id);
        }
    }
}
