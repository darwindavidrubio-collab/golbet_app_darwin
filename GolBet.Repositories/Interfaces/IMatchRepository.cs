using GolBet.Entities;
using GolBet.Entities.Enums;
using GolBet.Repositories.Interfaces;

namespace GolBet.Repositories.Interfaces;

public interface IMatchRepository : IGenericRepository<Match>
{
    Task<IEnumerable<Match>> GetAllWithTeamsAsync(MatchStatus? status = null, string? search = null);

    // AGREGA ESTA LÍNEA EXACTA:
    Task<Match?> GetMatchWithDetailsAsync(int id);
}