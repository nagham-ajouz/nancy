using Shared.BaseClasses;

namespace Shared.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)                          throw new ArgumentException("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required.");

        Amount   = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}