using APBD_CW7.Models;
using APBD_CW7.Models.DTOs;

namespace APBD_CW7.Services;

public interface IDBService
{
    Task<IEnumerable<TripsDTO>> GetTripsAsync();
    Task<List<TripPerClientDTO>?> GetTripsForClientAsync(int clientId);
    Task<int> CreateClientAsync(ClientsDTO clients);
    Task<bool> RegisterClientToTripAsync(int clientId, int tripId);
    Task<bool> RemoveClientFromTripAsync(int clientId, int tripId);
}