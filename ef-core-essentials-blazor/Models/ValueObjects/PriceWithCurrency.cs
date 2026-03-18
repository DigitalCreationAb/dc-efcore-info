using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ef_core_essentials_blazor.Models.ValueObjects;

/// <summary>
/// Custom value object for price with currency.
/// [Owned] makes EF Core auto-configure this as an owned type on every entity that uses it.
/// Data annotations set column names and constraints once here rather than in every configuration class.
/// </summary>
[Owned]
public record PriceWithCurrency
{
    [Column("Price")]
    [Precision(18, 2)]
    public decimal Amount { get; init; }

    [Column("Currency")]
    [MaxLength(3)]
    public string Currency { get; init; } = "SEK";

    public PriceWithCurrency() { }

    public PriceWithCurrency(decimal amount, string currency = "SEK")
    {
        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
