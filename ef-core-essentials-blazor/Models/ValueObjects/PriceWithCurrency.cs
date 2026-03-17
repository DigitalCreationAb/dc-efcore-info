namespace ef_core_essentials_blazor.Models.ValueObjects;

/// <summary>
/// Custom value object for price with currency
/// </summary>
public record PriceWithCurrency
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "SEK";

    public PriceWithCurrency() { }

    public PriceWithCurrency(decimal amount, string currency = "SEK")
    {
        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
