namespace LongWayHome.Domain.Games;

/// <summary>Why a Player action was refused. Player mistakes are results, never exceptions.</summary>
public enum Rejection
{
    NoTarget,
    AlreadyTilled,
    TooTired,
    NothingToDo,
    NotTilled,
    TileOccupied,
    NoSeed,
    NoPlanting,
    NotRipe,
    AlreadyWatered,
    CanEmpty,
    TooLate,
    OutOfSeason,
    NothingToShip,
    NotOffered,
    NotEnoughGold,
    BackpackFull,
    AreaClosed,
    OpeningOver,
    NothingToSell,
    NotSellable,
    NodeInTheWay,
    NeedsGrothAxe,
    AlreadyDelivered,
    NothingToDeliver,
}
