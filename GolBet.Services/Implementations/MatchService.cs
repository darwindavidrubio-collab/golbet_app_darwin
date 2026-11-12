// GolBet.Services/Implementations/MatchService.cs
using AutoMapper;
using GolBet.Entities;
using GolBet.Entities.Enums;
using GolBet.Repositories.Interfaces;
using GolBet.Services.DTOs;
using GolBet.Services.Interfaces;
using GolBet.Services.Helpers;

namespace GolBet.Services.Implementations;

public class MatchService : IMatchService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IBetRepository _betRepository; // [Módulo 9]
    private readonly IMapper _mapper;

    public MatchService(
        IMatchRepository matchRepository,
        IBetRepository betRepository,
        IMapper mapper)
    {
        _matchRepository = matchRepository;
        _betRepository = betRepository;
        _mapper = mapper;
    }

    // [Módulos 4, 5 y Taller]
    public async Task<IEnumerable<MatchDto>> GetAllAsync(MatchStatus? status = null, string? search = null)
    {
        var matches = await _matchRepository.GetAllWithTeamsAsync(status, search);
        return _mapper.Map<IEnumerable<MatchDto>>(matches);
    }

    // [Módulo 4 y 8] - Obtener por ID para el detalle (incluyendo apuestas para el contador)
    public async Task<MatchDetailDto?> GetByIdAsync(int id)
    {
        var match = await _matchRepository.GetMatchWithDetailsAsync(id);
        return match is null ? null : _mapper.Map<MatchDetailDto>(match);
    }

    // [Módulo 4 y 6] - Obtener para editar y convertir la hora UTC a hora de Colombia
    public async Task<MatchFormDto?> GetForEditAsync(int id)
    {
        var match = await _matchRepository.GetByIdAsync(id);
        if (match is null) return null;

        var dto = _mapper.Map<MatchFormDto>(match);
        dto.Date = dto.Date.ToColombiaTime();
        return dto;
    }

    // [Módulo 4 y 6] - Crear partido y convertir la hora de Colombia a UTC antes de guardar
    public async Task CreateAsync(MatchFormDto dto)
    {
        ValidateBusinessRules(dto);
        var match = _mapper.Map<Match>(dto);
        match.Date = dto.Date.ToUtcFromColombia();
        await _matchRepository.AddAsync(match);
    }

    // [Módulo 4 y 6] - Actualizar partido con conversión de hora UTC
    public async Task UpdateAsync(MatchFormDto dto)
    {
        ValidateBusinessRules(dto);
        var match = await _matchRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Match {dto.Id} not found.");

        _mapper.Map(dto, match);
        match.Date = dto.Date.ToUtcFromColombia();
        await _matchRepository.UpdateAsync(match);
    }

    // [Módulo 4] - Desactivar partido
    public async Task DeactivateAsync(int id)
    {
        await _matchRepository.DeactivateAsync(id);
    }

    // ==========================================
    // [MÓDULO 9] - INICIAR Y CERRAR PARTIDOS
    // ==========================================
    public async Task StartMatchAsync(int id)
    {
        var match = await _matchRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Match {id} not found.");

        if (match.Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Solo un partido programado puede iniciarse.");

        match.Status = MatchStatus.InProgress; // betting closes (rule #1 reads this)
        await _matchRepository.UpdateAsync(match);
    }

    public async Task<ResolutionSummaryDto> CloseMatchAsync(MatchResultFormDto dto)
    {
        var match = await _matchRepository.GetByIdAsync(dto.MatchId)
            ?? throw new KeyNotFoundException($"Match {dto.MatchId} not found.");

        // State machine guards (first idempotence belt)
        if (match.Status == MatchStatus.Finished)
            throw new InvalidOperationException("Este partido ya fue cerrado y sus apuestas resueltas.");

        if (match.Status != MatchStatus.InProgress)
            throw new InvalidOperationException("El partido debe estar «En juego» para cerrarse. Inícialo primero.");

        // 1. Record the final score and the new state
        match.HomeGoals = dto.HomeGoals;
        match.AwayGoals = dto.AwayGoals;
        match.Status = MatchStatus.Finished;

        // 2. Compute the winning pick from the score
        var winningPick = GetWinningPick(dto.HomeGoals, dto.AwayGoals);

        // 3. Resolve every PENDING bet (second idempotence belt: only Pending arrive)
        var pendingBets = await _betRepository.GetPendingByMatchAsync(match.Id);
        var summary = new ResolutionSummaryDto { ResolvedBets = pendingBets.Count };

        foreach (var bet in pendingBets)
        {
            if (bet.Pick == winningPick)
            {
                bet.Status = BetStatus.Won;
                var payout = bet.Amount * bet.OddsAtPlacement; // FROZEN odds pay
                bet.User.Balance += payout;                    // the win lands
                summary.WonBets++;
                summary.TotalPaid += payout;
            }
            else
            {
                bet.Status = BetStatus.Lost; // stake was deducted at placement
            }
        }

        // 4. ONE save: the match, every bet and every balance - ONE transaction
        await _matchRepository.UpdateAsync(match);
        return summary;
    }

    private static BetPick GetWinningPick(int homeGoals, int awayGoals)
        => homeGoals > awayGoals ? BetPick.Home
         : homeGoals < awayGoals ? BetPick.Away
         : BetPick.Draw;

    // [Módulo 4 y 6] - Validaciones de negocio centralizadas
    private static void ValidateBusinessRules(MatchFormDto dto)
    {
        if (dto.HomeTeamId == dto.AwayTeamId)
            throw new InvalidOperationException("El equipo local y el visitante no pueden ser el mismo.");

        if (dto.Date.ToUtcFromColombia() <= DateTime.UtcNow)
            throw new InvalidOperationException("La fecha del partido debe ser futura.");
    }
}