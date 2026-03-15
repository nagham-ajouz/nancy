using System.Text.RegularExpressions;
using Shared.BaseClasses;

namespace FleetService.Domain.ValueObjects;

public class LicenseNumber : ValueObject
{
    public string Value { get; }

    // Format: two uppercase letters + six digits, e.g. "LB123456"
    private static readonly Regex _format = new(@"^[A-Z]{2}\d{6}$");

    public LicenseNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))   
            throw new ArgumentException("License number required.");
        if (!_format.IsMatch(value.ToUpperInvariant())) 
            throw new ArgumentException($"Invalid license number format: {value}");
        Value = value.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}