// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using XmlSigningExample.Api.Models;
using XmlSigningExample.Api.Services;

namespace XmlSigningExample.Api.Controllers;

/// <summary>
/// Controller for XML signing operations with 2FA authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class XmlSigningController : ControllerBase
{
    private readonly IXmlSigningService _xmlSigningService;
    private readonly ILogger<XmlSigningController> _logger;
    
    public XmlSigningController(
        IXmlSigningService xmlSigningService,
        ILogger<XmlSigningController> logger)
    {
        _xmlSigningService = xmlSigningService ?? throw new ArgumentNullException(nameof(xmlSigningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Initiates XML signing process and generates a 2-character authentication code.
    /// The user must enter this code in their authenticator app to complete the signing.
    /// </summary>
    /// <param name="request">XML signing request</param>
    /// <returns>2-character authentication code and session ID</returns>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(AuthCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthCodeResponse>> InitiateSigning([FromBody] XmlSigningRequest request)
    {
        _logger.LogInformation("Initiating XML signing for user {Username}", request.Username);
        
        var result = await _xmlSigningService.InitiateSigningAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    
    /// <summary>
    /// Verifies the 2-character authentication code and completes the XML signing.
    /// Returns the signed XML document.
    /// </summary>
    /// <param name="request">Verification request with session ID and auth code</param>
    /// <returns>Signed XML document</returns>
    [HttpPost("verify-and-sign")]
    [ProducesResponseType(typeof(SignedXmlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignedXmlResponse>> VerifyAndSign([FromBody] VerifyAndSignRequest request)
    {
        _logger.LogInformation("Verifying and signing XML for session {SessionId}", request.SessionId);
        
        var result = await _xmlSigningService.VerifyAndSignAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
}
