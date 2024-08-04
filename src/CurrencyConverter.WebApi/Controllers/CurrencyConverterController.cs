using CurrencyConverter.Core.Domains.Entities;
using CurrencyConverter.Core.Domains.Services;
using CurrencyConverter.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CurrencyConverter.WebApi.Controllers;

/// <summary>
/// Controller for handling currency conversion operations.
/// </summary>
/// <remarks>
/// This controller provides endpoints for configuring exchange rates, clearing configuration, and converting currencies.
/// </remarks>
[ApiController]
[Route("api/v1/[controller]/[action]")]
public class CurrencyConverterController : ControllerBase
{
    #region Fields

    private readonly ICurrencyConverterServices _currencyConverterServices;

    #endregion

    #region Ctor

    public CurrencyConverterController(ICurrencyConverterServices currencyConverterServices)
    {
        _currencyConverterServices = currencyConverterServices;
    }

    #endregion

    /// <summary>
    /// Configures exchange rates.
    /// </summary>
    /// <param name="exchangeRateList">List of exchange rates.</param>
    /// <returns>An IActionResult representing the result of the operation.</returns>
    /// <response code="200">Exchange rates updated successfully.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public IActionResult Config(List<ExchangeRate> exchangeRateList)
    {
        try
        {
            _currencyConverterServices.UpdateConfiguration(exchangeRateList);
            return Ok("Exchange rates updated successfully.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error processing Json input.");
        }
    }

    /// <summary>
    /// Clears configuration.
    /// </summary>
    /// <returns>An IActionResult representing the result of the operation.</returns>
    /// <response code="200">Configuration cleared successfully.</response>
    /// <response code="400">Bad request.</response>
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public IActionResult Clear()
    {
        try
        {
            _currencyConverterServices.ClearConfiguration();
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Converts currency.
    /// </summary>
    /// <param name="request">The currency conversion request.</param>
    /// <returns>An IActionResult representing the result of the operation.</returns>
    /// <response code="200">Currency converted successfully.</response>
    /// <response code="400">Bad request.</response>
    /// <response code="404">Currency not found.</response>
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> ConvertCurrency([FromBody] CurrencyConversionRequest request)
    {
        try
        {
            var convertedAmount = await _currencyConverterServices.Convert(request);
            return Ok(new CurrencyConversionResponse { ConvertedAmount = convertedAmount });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}