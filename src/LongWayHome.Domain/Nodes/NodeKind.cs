namespace LongWayHome.Domain.Nodes;

/// <summary>
/// What stands at a node spawn point (canonical-values.md): <c>tree</c>, <c>stump</c>, <c>rock</c>,
/// <c>rosk_deposit</c>, <c>groth_deposit</c>, <c>glimmercap</c>, <c>debris</c>. Only tree, stump and rock are on the farm.
/// </summary>
public enum NodeKind
{
    Tree,
    Stump,
    Rock,
    RoskDeposit,
    GrothDeposit,
    Glimmercap,
    Debris,
}
