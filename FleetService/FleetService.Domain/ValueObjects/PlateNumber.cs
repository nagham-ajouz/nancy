using System.Text.RegularExpressions;
using Shared.BaseClasses;

namespace FleetService.Domain.ValueObjects;

public class PlateNumber : ValueObject
{
    public string Value { get; }

    // Accepts formats like "ABC-1234" or "AB-12345"
    private static readonly Regex _format = new(@"^[A-Z]{2,3}-\d{4,5}$");

    public PlateNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))   
            throw new ArgumentException("Plate number required.");
        if (!_format.IsMatch(value.ToUpperInvariant())) 
            throw new ArgumentException($"Invalid plate number format: {value}");
        Value = value.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}