namespace Shared.BaseClasses;

// Value objects have no identity — they are equal if all their values are equal.
// Example: two Money(100, "USD") objects are equal, two Drivers with the same name are not.
public abstract class ValueObject
{
    // Each value object must define which properties count for equality.
    // Example: Money returns Amount and Currency.
    protected abstract IEnumerable<object> GetEqualityComponents();

    // Two value objects are equal if all their equality components match.
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        ValueObject other = (ValueObject)obj;
        return other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());
    }

    // Combines all equality components into a single hash code.
    // Required whenever you override Equals — used by dictionaries and hash sets.
    public override int GetHashCode()
    {
        int hash = 1;
        foreach (object component in GetEqualityComponents())
        {
            hash = HashCode.Combine(hash, component?.GetHashCode() ?? 0);
        }
        return hash;
    }

    // Allows using == between two value objects instead of calling .Equals() manually.
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    // Allows using != between two value objects.
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}