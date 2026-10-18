// GolBet.Web/Controllers/MatchesController.cs
using GolBet.Entities.Enums;
using GolBet.Repositories.Data;
using GolBet.Services.DTOs;
using GolBet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GolBet.Web.Controllers;

public class MatchesController : Controller
{
    private readonly IMatchService _matchService;
    private readonly ITeamService _teamService;

    public MatchesController(IMatchService matchService, ITeamService teamService)
    {
        _matchService = matchService;
        _teamService = teamService;
    }

    // [Módulo 7] - AllowAnonymous 
    [AllowAnonymous]

    // [Módulo 4, 5] - Mod 5 trajo el filtro status y para el Taller trae el filtro search
    public async Task<IActionResult> Index(MatchStatus? status, string? search)
    {
        var matches = await _matchService.GetAllAsync(status, search);

        ViewBag.CurrentStatus = status; // [Módulo 5]
        ViewBag.CurrentSearch = search; // [Search]

        return View(matches);
    }

    // [Módulo 7] - Público para todo el mundo
    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var match = await _matchService.GetByIdAsync(id);
        if (match is null) return NotFound();
        return View(match);
    }

    // [Módulo 7] - Authorize 
    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Create()
    {
        await LoadTeamsAsync(); // [Módulo 4]
        return View(new MatchFormDto());
    }

    // [Módulo 7] - Candado de Admin
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Create(MatchFormDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadTeamsAsync();
            return View(dto);
        }
        try
        {
            await _matchService.CreateAsync(dto);
            TempData["Success"] = "Partido creado correctamente."; // [Módulo 4]
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadTeamsAsync();
            return View(dto);
        }
    }

    // [Módulo 7] - Candado de Admin
    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _matchService.GetForEditAsync(id);
        if (dto is null) return NotFound();

        await LoadTeamsAsync();
        return View(dto);
    }

    // [Módulo 7] - Candado de Admin
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Edit(MatchFormDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadTeamsAsync();
            return View(dto);
        }
        try
        {
            await _matchService.UpdateAsync(dto);
            TempData["Success"] = "Partido actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadTeamsAsync();
            return View(dto);
        }
    }

    // [Módulo 7] - Candado de Admin
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _matchService.DeactivateAsync(id);
        TempData["Success"] = "Partido desactivado.";
        return RedirectToAction(nameof(Index));
    }

    // [Módulo 4] - Carga los equipos para los SelectList de las vistas
    private async Task LoadTeamsAsync()
    {
        var teams = await _teamService.GetAllAsync();
        ViewBag.Teams = new SelectList(teams, "Id", "Name");
    }
}