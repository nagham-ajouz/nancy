namespace TripService.Application.DTOs.TripLog;

public class AddTripLogDto
{
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }
    public string   Address   { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public decimal? Speed     { get; set; }
}