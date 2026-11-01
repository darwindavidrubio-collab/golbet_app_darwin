// GolBet.Repositories/Implementations/MatchRepository.cs
using GolBet.Entities;
using GolBet.Entities.Enums;
using GolBet.Repositories.Data;
using GolBet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GolBet.Repositories.Implementations;

public class MatchRepository : GenericRepository<Match>, IMatchRepository
{
    private readonly AppDbContext _context;

    public MatchRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Match>> GetAllWithTeamsAsync(MatchStatus? status = null, string? search = null)
    {
        var query = _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                m.HomeTeam.Name.Contains(search) ||
                m.AwayTeam.Name.Contains(search));
        }

        return await query.OrderBy(m => m.Date).ToListAsync();
    }

    // ==========================================
    // NUEVO MÉTODO PARA EL DETALLE DEL PARTIDO
    // ==========================================
    public async Task<Match?> GetMatchWithDetailsAsync(int id)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Bets) 
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}