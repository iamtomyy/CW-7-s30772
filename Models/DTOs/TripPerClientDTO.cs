namespace APBD_CW7.Models;

public class TripPerClientDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public List<string> Countries { get; set; } = new();
}
