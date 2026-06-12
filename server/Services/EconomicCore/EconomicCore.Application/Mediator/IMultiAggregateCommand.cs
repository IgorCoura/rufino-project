namespace EconomicCore.Application.Mediator;

/// <summary>
/// Marker for a command whose transaction mutates MORE THAN ONE aggregate in a single SaveEntitiesAsync —
/// a sanctioned exception to the one-aggregate-per-transaction rule, requiring justification in the BC's
/// CLAUDE.md. There is no TransactionBehavior bound to it while every handler keeps exactly one save:
/// the marker documents the exception and is the extension point if such a behavior becomes necessary.
/// </summary>
public interface IMultiAggregateCommand;
