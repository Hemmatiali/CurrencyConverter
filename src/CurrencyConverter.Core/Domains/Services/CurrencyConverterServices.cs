using System.Collections.Concurrent;
using CurrencyConverter.Core.Common;
using CurrencyConverter.Core.Domains.Entities;
using CurrencyConverter.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Core.Domains.Services;

/// <inheritdoc cref="ICurrencyConverterServices"/>
public class CurrencyConverterServices : ICurrencyConverterServices
{
    #region Fields

    // Thread-safe lock for read/write operations
    private readonly ReaderWriterLockSlim _lock = new();

    // Concurrent dictionary to store exchange rates
    private readonly ConcurrentDictionary<string, Dictionary<string, double>> _exchangeRates = new();

    // Memory cache for storing conversion results
    private readonly IMemoryCache _cache;

    // Set to keep track of cache keys
    private readonly HashSet<string> _cacheKeys = new();

    // Graph representing currency relationships
    private CurrencyGraph _graph = new();

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyConverterServices"/> class.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    public CurrencyConverterServices(IMemoryCache cache)
    {
        _cache = cache;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    public void ClearConfiguration()
    {
        // Execute write lock to clear configuration
        ExecuteWriteLock(() =>
        {
            _exchangeRates.Clear(); // Clear exchange rates
            _graph = new CurrencyGraph(); // Reset the graph
            ClearCache(); // Clear the cache
        });
    }

    /// <summary>
    /// Clears the conversion cache.
    /// </summary>
    public void ClearCache()
    {
        // Remove each cache key from the cache
        foreach (var key in _cacheKeys)
        {
            _cache.Remove(key);
        }
        _cacheKeys.Clear(); // Clear the set of cache keys
    }

    /// <inheritdoc />
    public void UpdateConfiguration(IEnumerable<ExchangeRate> conversionRates)
    {
        // Clear existing configuration before updating
        ClearConfiguration();

        // Execute write lock to update configuration
        ExecuteWriteLock(() =>
        {
            // Add each exchange rate to the internal data structure
            foreach (var rate in conversionRates)
            {
                AddExchangeRate(rate);
            }
            // Rebuild the currency graph
            BuildCurrencyGraph();
        });
    }


    /// <inheritdoc/>
    public async Task<decimal> Convert(CurrencyConversionRequest request)
    {
        // Validate the conversion request
        ValidateRequest(request);

        // Execute read lock to perform the conversion
        return ExecuteReadLock(() =>
        {
            // Attempt direct conversion first
            if (TryDirectConversion(request, out var directResult))
            {
                return directResult;
            }

            // Find the shortest path in the graph if direct conversion is not possible
            var path = FindShortestPath(_graph, request.FromCurrency, request.ToCurrency);
            if (path == null || path.Count == 0)
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.ItemNotFoundMsg, "Conversion Path"));
            }

            // Calculate the conversion based on the path
            return CalculateConversion(path, (decimal)request.Amount);
        });
    }

    /// <summary>
    /// Adds an exchange rate to the internal data structure.
    /// </summary>
    /// <param name="rate">The exchange rate to add.</param>
    private void AddExchangeRate(ExchangeRate rate)
    {
        // Extract data from the rate object
        var fromCurrency = rate.FromCurrency.Value;
        var toCurrency = rate.ToCurrency.Value;
        var conversionRate = rate.Rate.Value;
        var inverseRate = rate.InverseRate.Value;

        // Add the direct exchange rate
        AddRate(fromCurrency, toCurrency, conversionRate);
        // Add the inverse exchange rate
        AddRate(toCurrency, fromCurrency, inverseRate);
    }

    /// <summary>
    /// Adds a single exchange rate to the dictionary.
    /// </summary>
    /// <param name="fromCurrency">The source currency.</param>
    /// <param name="toCurrency">The target currency.</param>
    /// <param name="rate">The exchange rate.</param>
    private void AddRate(string fromCurrency, string toCurrency, double rate)
    {
        // Ensure the fromCurrency dictionary exists
        if (!_exchangeRates.ContainsKey(fromCurrency))
        {
            _exchangeRates[fromCurrency] = new Dictionary<string, double>();
        }
        // Add the rate to the dictionary
        _exchangeRates[fromCurrency][toCurrency] = rate;
    }

    /// <summary>
    /// Builds the currency graph based on the existing exchange rates.
    /// </summary>
    private void BuildCurrencyGraph()
    {
        // Iterate over all exchange rates and add them to the graph
        foreach (var fromCurrency in _exchangeRates.Keys)
        {
            foreach (var toCurrency in _exchangeRates[fromCurrency].Keys)
            {
                _graph.AddEdge(fromCurrency, toCurrency); // Add edge from source to target
                _graph.AddEdge(toCurrency, fromCurrency); // Add edge from target to source
            }
        }
    }


    /// <summary>
    /// Validates the currency conversion request.
    /// </summary>
    /// <param name="request">The conversion request.</param>
    private void ValidateRequest(CurrencyConversionRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request)); // Throw an exception if the request is null
        }

        if (string.IsNullOrWhiteSpace(request.FromCurrency))
        {
            throw new ArgumentException(string.Format(ErrorMessages.InputCannotBeNullWhiteSpaceMsg, nameof(request.FromCurrency)), nameof(request.FromCurrency)); // Throw an exception if FromCurrency is invalid
        }

        if (string.IsNullOrWhiteSpace(request.ToCurrency))
        {
            throw new ArgumentException(string.Format(ErrorMessages.InputCannotBeNullWhiteSpaceMsg, nameof(request.ToCurrency)), nameof(request.ToCurrency)); // Throw an exception if ToCurrency is invalid
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), string.Format(ErrorMessages.InputMustBePositiveMsg, nameof(request.Amount))); // Throw an exception if the amount is non-positive
        }

        if (request.FromCurrency == request.ToCurrency)
        {
            throw new ArgumentException("FromCurrency and ToCurrency cannot be the same."); // Throw an exception if the currencies are the same
        }
    }

    /// <summary>
    /// Executes the given action within a write lock.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    private void ExecuteWriteLock(Action action)
    {
        _lock.EnterWriteLock(); // Enter write lock
        try
        {
            action(); // Execute the action
        }
        finally
        {
            _lock.ExitWriteLock(); // Exit write lock
        }
    }

    /// <summary>
    /// Executes the given function within a read lock.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result of the function.</returns>
    private T ExecuteReadLock<T>(Func<T> func)
    {
        _lock.EnterReadLock(); // Enter read lock
        try
        {
            return func(); // Execute the function and return the result
        }
        finally
        {
            _lock.ExitReadLock(); // Exit read lock
        }
    }

    /// <summary>
    /// Tries to perform a direct currency conversion.
    /// </summary>
    /// <param name="request">The conversion request.</param>
    /// <param name="result">The result of the conversion.</param>
    /// <returns>True if the direct conversion was successful; otherwise, false.</returns>
    private bool TryDirectConversion(CurrencyConversionRequest request, out decimal result)
    {
        result = 0;

        // Check if the direct conversion rate exists
        if (request is { FromCurrency: not null, ToCurrency: not null } && _exchangeRates.TryGetValue(request.FromCurrency, out var rates) && rates.TryGetValue(request.ToCurrency, out var rate))
        {
            result = (decimal)(request.Amount * rate); // Calculate the result
            return true; // Return true indicating a successful conversion
        }

        return false; // Return false indicating the conversion was not possible
    }

    /// <summary>
    /// Finds the shortest path between two currencies in the given graph.
    /// </summary>
    /// <param name="graph">The currency graph.</param>
    /// <param name="startCurrency">The starting currency.</param>
    /// <param name="endCurrency">The ending currency.</param>
    /// <returns>A list of strings representing the shortest path between the start and end currencies. Returns null if no path is found.</returns>
    private List<string> FindShortestPath(CurrencyGraph graph, string startCurrency, string endCurrency)
    {
        var distances = new Dictionary<string, int>(); // Dictionary to track the shortest distance to each currency
        var previous = new Dictionary<string, string>(); // Dictionary to track the previous node in the path
        var priorityQueue = new PriorityQueue<string, int>(); // Priority queue to process nodes

        // Initialize distances for each currency
        foreach (var currency in graph.AdjacencyList.Keys)
        {
            distances[currency] = int.MaxValue;
        }
        distances[startCurrency] = 0; // Set the start currency distance to 0
        priorityQueue.Enqueue(startCurrency, 0); // Enqueue the start currency

        // Process nodes in the priority queue
        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue(); // Dequeue the node with the smallest distance

            // If we reached the end currency, reconstruct the path
            if (current == endCurrency)
            {
                return ReconstructPath(previous, endCurrency);
            }

            // Skip if the current currency is not in the adjacency list
            if (!graph.AdjacencyList.ContainsKey(current)) continue;

            // Iterate over neighbors of the current node
            foreach (var neighbor in graph.AdjacencyList[current])
            {
                var altDistance = distances[current] + 1; // Calculate alternative distance

                // Update the distance and previous node if a shorter path is found
                if (altDistance < distances[neighbor])
                {
                    distances[neighbor] = altDistance;
                    previous[neighbor] = current;
                    priorityQueue.Enqueue(neighbor, altDistance); // Enqueue the neighbor with updated distance
                }
            }
        }

        return null; // Return null if no path is found
    }

    /// <summary>
    /// Reconstructs the shortest path based on the previous dictionary and the end currency.
    /// </summary>
    /// <param name="previous">The dictionary containing the previous node for each currency.</param>
    /// <param name="endCurrency">The ending currency.</param>
    /// <returns>A list of strings representing the shortest path from the start currency to the end currency.</returns>
    private List<string> ReconstructPath(Dictionary<string, string> previous, string endCurrency)
    {
        var path = new List<string>(); // List to store the path
        var current = endCurrency; // Start from the end currency

        // Traverse the previous nodes to build the path
        while (current != null)
        {
            path.Insert(0, current); // Insert the current node at the beginning of the path
            current = previous.ContainsKey(current) ? previous[current] : null; // Move to the previous node
        }

        return path; // Return the reconstructed path
    }

    /// <summary>
    /// Calculates the conversion steps based on the provided conversion path and amount.
    /// </summary>
    /// <param name="path">The conversion path.</param>
    /// <param name="amount">The amount to convert.</param>
    /// <returns>The converted amount.</returns>
    private decimal CalculateConversion(List<string> path, decimal amount)
    {
        var cacheKey = GenerateCacheKeyForPath(path); // Generate a cache key for the path

        // Attempt to retrieve the conversion result from the cache
        if (_cache.TryGetValue(cacheKey, out decimal cachedResult))
        {
            return cachedResult;
        }

        decimal result = amount; // Initialize the result with the input amount

        // Iterate through the path to calculate the conversion
        for (var i = 0; i < path.Count - 1; i++)
        {
            var from = path[i];
            var to = path[i + 1];

            // Check if the direct exchange rate exists
            if (_exchangeRates.ContainsKey(from) && _exchangeRates[from].ContainsKey(to))
            {
                result *= (decimal)_exchangeRates[from][to]; // Multiply the result by the exchange rate
            }
            else if (_exchangeRates.ContainsKey(to) && _exchangeRates[to].ContainsKey(from))
            {
                result /= (decimal)_exchangeRates[to][from]; // Divide the result by the inverse rate
            }
            else
            {
                throw new Exception(string.Format(ErrorMessages.ItemNotFoundMsg, "Conversion Path")); // Throw an exception if the rate is not found
            }
        }

        // Store the result in the cache
        _cache.Set(cacheKey, result, GetCacheOptions());
        _cacheKeys.Add(cacheKey); // Add the cache key to the set

        return result; // Return the calculated result
    }

    /// <summary>
    /// Generates a cache key based on the provided conversion path.
    /// </summary>
    /// <param name="path">The conversion path.</param>
    /// <returns>The generated cache key.</returns>
    private string GenerateCacheKeyForPath(List<string> path)
    {
        return string.Join("-", path); // Join the path elements to create a unique cache key
    }

    /// <summary>
    /// Retrieves the cache options for storing conversion results.
    /// </summary>
    /// <returns>The memory cache entry options.</returns>
    private MemoryCacheEntryOptions GetCacheOptions()
    {
        // Configure cache entry options with a sliding expiration of 2 minutes
        return new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2));
    }

    //todo remove
    ///// <inheritdoc/>
    //public async Task<decimal> Convert(CurrencyConversionRequest request)
    //{
    //    _lock.EnterReadLock();
    //    try
    //    {
    //        // Validations
    //        if (request == null)
    //            throw new ArgumentNullException(nameof(request));

    //        if (string.IsNullOrWhiteSpace(request.FromCurrency))
    //            throw new ArgumentException(
    //                string.Format(ErrorMessages.InputCannotBeNullWhiteSpaceMsg, nameof(request.FromCurrency)),
    //                nameof(request.FromCurrency));

    //        if (string.IsNullOrWhiteSpace(request.ToCurrency))
    //            throw new ArgumentException(
    //                string.Format(ErrorMessages.InputCannotBeNullWhiteSpaceMsg, nameof(request.ToCurrency)),
    //                nameof(request.ToCurrency));

    //        if (request.Amount <= 0)
    //            throw new ArgumentOutOfRangeException(nameof(request.Amount),
    //                string.Format(ErrorMessages.InputMustBePositiveMsg, nameof(request.Amount)));

    //        if (request.FromCurrency == request.ToCurrency)
    //            return (decimal)request.Amount;

    //        // Retrieve data
    //        var fromCurrency = request.FromCurrency;
    //        var toCurrency = request.ToCurrency;

    //        // Check if the conversion is possible
    //        if (_exchangeRates.TryGetValue(fromCurrency, out var rates) && rates.TryGetValue(toCurrency, out var rate))
    //        {
    //            if (rate == 1.0)
    //                return (decimal)request.Amount;

    //            return (decimal)(request.Amount * rate);
    //        }

    //        //// Check if the inverse conversion is possible
    //        //if (_exchangeRates.TryGetValue(toIndex, out var inverseRates) && inverseRates.TryGetValue(fromIndex, out var inverseRate))
    //        //{
    //        //    if (inverseRate == 1.0)
    //        //        return request.Amount;

    //        //    return request.Amount / inverseRate;
    //        //}

    //        // Use Dijkstra's Algorithm to find shortest path based on graph
    //        var path = FindShortestPath(_graph, fromCurrency, toCurrency);

    //        if (path is { Count: > 0 })
    //        {
    //            return CalculateConversion(path, (decimal)request.Amount);
    //        }

    //        throw new InvalidOperationException(string.Format(ErrorMessages.ItemNotFoundMsg, "Conversion Path"));
    //    }
    //    catch (Exception e)
    //    {
    //        throw;
    //    }
    //    finally
    //    {
    //        _lock.ExitReadLock();
    //    }
    //}

    ///// <summary>
    ///// Adds an exchange rate to the internal data structure.
    ///// </summary>
    ///// <param name="rate">The exchange rate to add.</param>
    //private void AddExchangeRate(ExchangeRate rate)
    //{
    //    // Retrieve data
    //    var fromCurrency = rate.FromCurrency.Value;
    //    var toCurrency = rate.ToCurrency.Value;
    //    var conversionRate = rate.Rate.Value;
    //    var inverseRate = rate.InverseRate.Value;

    //    // Add exchange rate
    //    if (!_exchangeRates.ContainsKey(fromCurrency))
    //    {
    //        _exchangeRates[fromCurrency] = new Dictionary<string, double>();
    //    }
    //    _exchangeRates[fromCurrency][toCurrency] = conversionRate;

    //    // Add inverse exchange rate
    //    if (!_exchangeRates.ContainsKey(toCurrency))
    //    {
    //        _exchangeRates[toCurrency] = new Dictionary<string, double>();
    //    }
    //    _exchangeRates[toCurrency][fromCurrency] = inverseRate;
    //}

    ///// <summary>
    ///// Builds the currency graph based on the existing exchange rates.
    ///// </summary>
    //private void BuildCurrencyGraph()
    //{
    //    _graph = new CurrencyGraph();

    //    foreach (var fromCurrency in _exchangeRates.Keys)
    //    {
    //        foreach (var toCurrency in _exchangeRates[fromCurrency].Keys)
    //        {
    //            _graph.AddEdge(fromCurrency, toCurrency); // No weight needed 
    //            _graph.AddEdge(toCurrency, fromCurrency); // No weight needed 
    //        }
    //    }
    //}

    ///// <summary>
    ///// Finds the shortest path between two currencies in the given graph.
    ///// </summary>
    ///// <param name="graph">The currency graph.</param>
    ///// <param name="startCurrency">The starting currency.</param>
    ///// <param name="endCurrency">The ending currency.</param>
    ///// <returns>
    /////     A list of strings representing the shortest path between the start and end currencies.
    /////     Returns null if no path is found.
    ///// </returns>
    //private List<string> FindShortestPath(CurrencyGraph graph, string startCurrency, string endCurrency)
    //{
    //    var distances = new Dictionary<string, int>(); // Track hops instead of cumulative cost
    //    var previous = new Dictionary<string, string>();

    //    // Priority queue that prioritizes based on hops (distances)
    //    var priorityQueue = new PriorityQueue<string, int>();

    //    // Initialize distances
    //    foreach (var currency in graph.AdjacencyList.Keys)
    //    {
    //        distances[currency] = int.MaxValue;
    //    }
    //    distances[startCurrency] = 0;
    //    priorityQueue.Enqueue(startCurrency, 0); // Add start node with hop count 0

    //    while (priorityQueue.Count > 0)
    //    {
    //        var current = priorityQueue.Dequeue();

    //        if (current == endCurrency) // Found a conversion path
    //        {
    //            return ReconstructPath(previous, endCurrency);
    //        }

    //        if (!graph.AdjacencyList.ContainsKey(current)) continue;

    //        foreach (var neighbor in graph.AdjacencyList[current])
    //        {
    //            var altDistance = distances[current] + 1; // Increment hops by 1

    //            if (altDistance < distances[neighbor])
    //            {
    //                distances[neighbor] = altDistance;
    //                previous[neighbor] = current;

    //                // Enqueue or update priority based on new hop distance
    //                priorityQueue.Enqueue(neighbor, altDistance);
    //            }
    //        }
    //    }

    //    return null; // If no path is found
    //}

    ///// <summary>
    ///// Reconstructs the shortest path based on the previous dictionary and the end currency.
    ///// </summary>
    ///// <param name="previous">The dictionary containing the previous node for each currency.</param>
    ///// <param name="endCurrency">The ending currency.</param>
    ///// <returns>
    /////     A list of strings representing the shortest path from the start currency to the end currency.
    ///// </returns>
    //private List<string> ReconstructPath(Dictionary<string, string> previous, string endCurrency)
    //{
    //    var path = new List<string>();
    //    var current = endCurrency;
    //    while (current != null)
    //    {
    //        path.Insert(0, current); // Build path in reverse
    //        current = previous.ContainsKey(current) ? previous[current] : null;
    //    }
    //    return path;
    //}

    ///// <summary>
    ///// Calculates the conversion steps based on the provided conversion path and amount.
    ///// </summary>
    ///// <param name="path">The conversion path.</param>
    ///// <param name="amount">The amount to convert.</param>
    ///// <returns>The converted amount.</returns>
    //private decimal CalculateConversion(List<string> path, decimal amount)
    //{
    //    string cacheKey = GenerateCacheKeyForPath(path);

    //    // Attempt to retrieve from cache
    //    if (_cache.TryGetValue(cacheKey, out decimal cachedResult))
    //    {
    //        return cachedResult;
    //    }

    //    decimal result = amount;
    //    for (var i = 0; i < path.Count - 1; i++)
    //    {
    //        var from = path[i];
    //        var to = path[i + 1];

    //        // Check if the direct exchange rate exists
    //        if (_exchangeRates.ContainsKey(from) && _exchangeRates[from].ContainsKey(to))
    //        {
    //            result *= (decimal)_exchangeRates[from][to];
    //        }
    //        else // Handle missing direct rate - try the inverse
    //        {
    //            if (_exchangeRates.ContainsKey(to) && _exchangeRates[to].ContainsKey(from))
    //            {
    //                result /= (decimal)_exchangeRates[to][from]; // Calculate using inverse rate
    //            }
    //            else
    //            {
    //                throw new Exception(string.Format(ErrorMessages.ItemNotFoundMsg, "Conversion Path"));
    //            }
    //        }
    //    }

    //    // Store in cache
    //    _cache.Set(cacheKey, result, GetCacheOptions());
    //    _cacheKeys.Add(cacheKey);
    //    return result;
    //}

    ///// <summary>
    ///// Generates a cache key based on the provided conversion path.
    ///// </summary>
    ///// <param name="path">The conversion path.</param>
    ///// <returns>The generated cache key.</returns>
    //private string GenerateCacheKeyForPath(List<string> path)
    //{
    //    return string.Join("-", path);
    //}

    ///// <summary>
    ///// Retrieves the cache options for storing conversion results.
    ///// </summary>
    ///// <returns>The memory cache entry options.</returns>
    //private MemoryCacheEntryOptions GetCacheOptions()
    //{
    //    // Configure expiration
    //    return new MemoryCacheEntryOptions()
    //        .SetSlidingExpiration(TimeSpan.FromMinutes(2));
    //}

    ///// <summary>
    ///// Clears the conversion cache.
    ///// </summary>
    //public void ClearCache()
    //{
    //    foreach (var key in _cacheKeys)
    //    {
    //        _cache.Remove(key);
    //    }
    //    _cacheKeys.Clear();
    //}

    #endregion

}