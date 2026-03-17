namespace ef_core_essentials_blazor.Models;

/// <summary>
/// Interface for soft delete pattern - entities are never physically deleted
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
