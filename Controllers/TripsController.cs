using APBD_CW7.Models;
using APBD_CW7.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_CW7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController(IDBService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await service.GetTripsAsync();
        return Ok(trips);
    }

    [HttpGet("/clients/{id}/trips")]
    public async Task<IActionResult> GetTripsForClient(int id)
    {
        var trips = await service.GetTripsForClientAsync(id);

        if (trips is null)
        {
            return NotFound($"Klient o ID {id} nie istnieje.");
        }

        return Ok(trips);
    }
    
    
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] ClientsDTO client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Pesel))
        {
            return BadRequest("Wymagane pola: FirstName, LastName, Email, Pesel.");
        }

        try
        {
            var newId = await service.CreateClientAsync(client);
            return Created($"clients/{newId}", new { IdClient = newId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd serwera: {ex.Message}");
        }
    }

    
    [HttpPut("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
    {
        try
        {
            var register = await service.RegisterClientToTripAsync(id, tripId);
            return register ? Ok("Klient został zapisany na wycieczkę.") : StatusCode(500, "Nie udało się zapisać klienta.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    
    [HttpDelete("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
    {
        try
        {
            var removed = await service.RemoveClientFromTripAsync(id, tripId);
            return removed ? Ok("Rejestracja została usunięta.") : StatusCode(500, "Nie udało się usunąć rejestracji.");
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

}

