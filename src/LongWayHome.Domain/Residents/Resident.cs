namespace LongWayHome.Domain.Residents;

/// <summary>A townsperson living in Ember Bay (child of Game, one per <see cref="ResidentId"/>).</summary>
public sealed class Resident
{
    public Resident(ResidentId id, bool present)
    {
        Id = id;
        Present = present;
    }

    public ResidentId Id { get; }

    /// <summary>Whether the Resident lives in Ember Bay now. Nothing sets it back to false (BR-R1).</summary>
    public bool Present { get; }
}
