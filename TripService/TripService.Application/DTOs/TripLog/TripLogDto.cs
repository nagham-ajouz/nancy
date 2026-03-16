namespace TripService.Application.DTOs.TripLog;

public class TripLogDto
{
    public Guid      Id        { get; set; }
    public Guid      TripId    { get; set; }
    public double    Latitude  { get; set; }
    public double    Longitude { get; set; }
    public string    Address   { get; set; } = null!;
    public DateTime  Timestamp { get; set; }
    public decimal?  Speed     { get; set; }
}