namespace CurrencyConverter.Core.Domains.Entities;

/// <summary>
/// Represents a graph structure for currency conversion.
/// </summary>
public sealed class CurrencyGraph
{
    // Fields
    public Dictionary<string, List<string>> AdjacencyList { get; set; } = new();

    // Methods

    /// <summary>
    /// Adds an edge between two currencies in the graph.
    /// </summary>
    /// <param name="fromCurrency">The starting currency of the edge.</param>
    /// <param name="toCurrency">The destination currency of the edge.</param>
    public void AddEdge(string fromCurrency, string toCurrency)
    {
        if (!AdjacencyList.ContainsKey(fromCurrency))
        {
            AdjacencyList[fromCurrency] = new List<string>();
        }
        // Aiming for the shortest path in steps, no additional weights are needed
        // Check for duplicates before adding
        if (!AdjacencyList[fromCurrency].Contains(toCurrency))
        {
            AdjacencyList[fromCurrency].Add(toCurrency);
        }
    }
}