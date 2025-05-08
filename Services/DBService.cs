using APBD_CW7.Models;
using APBD_CW7.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_CW7.Services;

public class DBService(IConfiguration config) : IDBService
{
    public async Task<IEnumerable<TripsDTO>> GetTripsAsync()
    {
        var result = new List<TripsDTO>();

        var sql = (@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS Country
            FROM Trip t
            LEFT JOIN Country_Trip ct ON ct.IdTrip = t.IdTrip
            LEFT JOIN Country c ON c.IdCountry = ct.IdCountry
            ORDER BY t.IdTrip;");

        await using var connection = new SqlConnection(config.GetConnectionString("Default"));
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        TripsDTO? trip = null;

        while (await reader.ReadAsync())
        {
            {
                trip = new TripsDTO
                {
                    IdTrip = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                };
                result.Add(trip);
            }

            trip!.Countries.Add(reader.GetString(6));
        }

        return result;
    }
    
    public async Task<List<TripPerClientDTO>?> GetTripsForClientAsync(int clientId)
{
    using var connection = new SqlConnection(config.GetConnectionString("Default"));
    await connection.OpenAsync();

    var isClientExisting = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", connection);
    isClientExisting.Parameters.AddWithValue("@id", clientId);
    var exists = await isClientExisting.ExecuteScalarAsync();
    if (exists is null)
    {
        return null;
    }

    var command = new SqlCommand(@"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               ct.RegisteredAt, ct.PaymentDate, c.Name AS CountryName
        FROM Client_Trip ct
        JOIN Trip t ON t.IdTrip = ct.IdTrip
        JOIN Country_Trip ctr ON ctr.IdTrip = t.IdTrip
        JOIN Country c ON c.IdCountry = ctr.IdCountry
        WHERE ct.IdClient = @id
        ORDER BY t.IdTrip;", connection);
    command.Parameters.AddWithValue("@id", clientId);

    var reader = await command.ExecuteReaderAsync();
    var tripDict = new Dictionary<int, TripPerClientDTO>();

    while (await reader.ReadAsync())
    {
        var tripId = reader.GetInt32(0);

        if (!tripDict.ContainsKey(tripId))
        {
            tripDict[tripId] = new TripPerClientDTO
            {
                IdTrip = tripId,
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = DateTime.ParseExact(reader.GetInt32(6).ToString(), "yyyyMMdd", null),
                PaymentDate = reader.IsDBNull(7) ? null : DateTime.ParseExact(reader.GetInt32(7).ToString(), "yyyyMMdd", null),
                Countries = new List<string>()
            };
        }

        tripDict[tripId].Countries.Add(reader.GetString(8));
    }

    await reader.CloseAsync();
    return tripDict.Values.ToList();
    }
    
    
    
    

    public async Task<int> CreateClientAsync(ClientsDTO client)
    {
        using var connection = new SqlConnection(config.GetConnectionString("Default"));
        await connection.OpenAsync();

        var command = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);", connection);

        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", string.IsNullOrWhiteSpace(client.Telephone) ? DBNull.Value : client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        var newId = (int)(await command.ExecuteScalarAsync() ?? throw new Exception("Insert failed"));
        return newId;
    }



    public async Task<bool> RegisterClientToTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(config.GetConnectionString("Default"));
        await connection.OpenAsync();

        var isClientExisting = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", connection);
        isClientExisting.Parameters.AddWithValue("@id", clientId);
        if (await isClientExisting.ExecuteScalarAsync() is null)
        {
            throw new Exception($"Klient o ID {clientId} nie istnieje.");
        }


        var isTripExisting = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId", connection);
        isTripExisting.Parameters.AddWithValue("@tripId", tripId);
        var maxPeopleAmount = await isTripExisting.ExecuteScalarAsync();
        if (maxPeopleAmount is null)
        {
            throw new Exception($"Wycieczka o ID {tripId} nie istnieje.");
        }
        
        int maxPeople = (int)maxPeopleAmount;

        var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", connection);
        countCmd.Parameters.AddWithValue("@tripId", tripId);
        int currentCount = (int)await countCmd.ExecuteScalarAsync();

        if (currentCount >= maxPeople)
        {
            throw new Exception("Osiągnięto maksymalną liczbę uczestników.");
        }

        var checkExisting = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", connection);
        checkExisting.Parameters.AddWithValue("@id", clientId);
        checkExisting.Parameters.AddWithValue("@tripId", tripId);
        if (await checkExisting.ExecuteScalarAsync() is not null)
        {
            throw new Exception("Klient już zapisany na tę wycieczkę.");
        }

        var now = DateTime.Now.ToString("yyyyMMdd");
        var command = new SqlCommand(@"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
        VALUES (@id, @tripId, @registeredAt);", connection);
        command.Parameters.AddWithValue("@id", clientId);
        command.Parameters.AddWithValue("@tripId", tripId);
        command.Parameters.AddWithValue("@registeredAt", now);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }
    
    public async Task<bool> RemoveClientFromTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(config.GetConnectionString("Default"));
        await connection.OpenAsync();

        var isRegistrationExisting = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", connection);
        isRegistrationExisting.Parameters.AddWithValue("@id", clientId);
        isRegistrationExisting.Parameters.AddWithValue("@tripId", tripId);
        var exists = await isRegistrationExisting.ExecuteScalarAsync();
        if (exists is null)
        {
            throw new Exception("Taka rejestracja nie istnieje.");
        }

        var deleteRegistration = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", connection);
        deleteRegistration.Parameters.AddWithValue("@id", clientId);
        deleteRegistration.Parameters.AddWithValue("@tripId", tripId);

        var rows = await deleteRegistration.ExecuteNonQueryAsync();
        return rows > 0;
    }

    
}