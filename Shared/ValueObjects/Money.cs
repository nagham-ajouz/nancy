using Shared.BaseClasses;

namespace Shared.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }
    
    private static readonly HashSet<string> ValidCurrencies = new()
    {
        "USD", "EUR", "LBP", "GBP", "SAR", "AED"
    };

    public Money(decimal amount, string currency)
    {
        if (amount < 0)                          
            throw new ArgumentException("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) 
            throw new ArgumentException("Currency required.");
        
        var normalized = currency.ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
            throw new ArgumentException("Currency must be a 3-letter ISO code.");

        if (!ValidCurrencies.Contains(normalized))
            throw new ArgumentException($"Invalid currency: {currency}");

        Amount   = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}