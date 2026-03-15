using Shared.BaseClasses;

namespace Shared.ValueObjects;

public class Location : ValueObject
{
    public double Latitude  { get; }
    public double Longitude { get; }
    public string Address   { get; }

    public Location(double latitude, double longitude, string address)
    {
        if (latitude  < -90  || latitude  > 90)  
            throw new ArgumentException("Invalid latitude.");
        
        if (longitude < -180 || longitude > 180) 
            throw new ArgumentException("Invalid longitude.");
        
        if (string.IsNullOrWhiteSpace(address))  
            throw new ArgumentException("Address required.");

        Latitude  = latitude;
        Longitude = longitude;
        Address   = address;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Address;
    }
}