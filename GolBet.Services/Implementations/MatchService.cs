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
    private readonly IMapper _mapper;

    public MatchService(IMatchRepository matchRepository, IMapper mapper)
    {
        _matchRepository = matchRepository;
        _mapper = mapper;
    }

    // [Módulos 4, 5 y Taller] - El método base es del Mod 4, 'status' se agregó en Mod 5, y 'search' en el Taller
    public async Task<IEnumerable<MatchDto>> GetAllAsync(MatchStatus? status = null, string? search = null)
    {
        var matches = await _matchRepository.GetAllWithTeamsAsync(status, search);
        return _mapper.Map<IEnumerable<MatchDto>>(matches);
    }

    // [Módulo 4] - Obtener por ID para el detalle
    public async Task<MatchDetailDto?> GetByIdAsync(int id)
    {
        // Cambiamos a GetMatchWithDetailsAsync para incluir la lista de apuestas y que el contador funcione
        var match = await _matchRepository.GetMatchWithDetailsAsync(id);
        return match is null ? null : _mapper.Map<MatchDetailDto>(match);
    }

    // [Módulo 4 y 6] - Obtener para editar y convertir la hora UTC a hora de Colombia (Mod 6)
    public async Task<MatchFormDto?> GetForEditAsync(int id)
    {
        var match = await _matchRepository.GetByIdAsync(id);
        if (match is null) return null;

        var dto = _mapper.Map<MatchFormDto>(match);
        dto.Date = dto.Date.ToColombiaTime(); // [Módulo 6]
        return dto;
    }

    // [Módulo 4 y 6] - Crear partido y convertir la hora de Colombia a UTC antes de guardar (Mod 6)
    public async Task CreateAsync(MatchFormDto dto)
    {
        ValidateBusinessRules(dto);
        var match = _mapper.Map<Match>(dto);
        match.Date = dto.Date.ToUtcFromColombia(); // [Módulo 6]
        await _matchRepository.AddAsync(match);
    }

    // [Módulo 4 y 6] - Actualizar partido con conversión de hora UTC
    public async Task UpdateAsync(MatchFormDto dto)
    {
        ValidateBusinessRules(dto);
        var match = await _matchRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Match {dto.Id} not found.");

        _mapper.Map(dto, match);
        match.Date = dto.Date.ToUtcFromColombia(); // [Módulo 6]
        await _matchRepository.UpdateAsync(match);
    }

    // [Módulo 4] - Desactivar partido
    public async Task DeactivateAsync(int id)
    {
        await _matchRepository.DeactivateAsync(id);
    }

    // [Módulo 4 y 6] - Validaciones de negocio centralizadas
    private static void ValidateBusinessRules(MatchFormDto dto)
    {
        if (dto.HomeTeamId == dto.AwayTeamId)
            throw new InvalidOperationException("El equipo local y el visitante no pueden ser el mismo.");

        if (dto.Date.ToUtcFromColombia() <= DateTime.UtcNow)
            throw new InvalidOperationException("La fecha del partido debe ser futura.");
    }
}