﻿namespace APBD_CW7.Models.DTOs;

public class TripsDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }

    
    public List<string> Countries { get; set; } = new();
}