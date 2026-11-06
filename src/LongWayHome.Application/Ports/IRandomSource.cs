namespace LongWayHome.Application.Ports;

/// <summary>Supplies random rolls for the game's chance rules (BR-G4).</summary>
public interface IRandomSource
{
    /// <summary>An integer from 0 to 99 inclusive, uniformly.</summary>
    int NextPercent();
}
