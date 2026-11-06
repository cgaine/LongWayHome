using LongWayHome.Application.Data;

namespace LongWayHome.Application.Ports;

/// <summary>Supplies the game's validated reference data.</summary>
public interface IGameDataSource
{
    GameData Load();
}
