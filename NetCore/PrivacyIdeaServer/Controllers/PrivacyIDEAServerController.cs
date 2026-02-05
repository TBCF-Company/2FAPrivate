// SPDX-License-Identifier: AGPL-3.0-or-later
// 2017-08-24 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//           REST API to add and delete remote privacyIDEA servers.
// Ported to .NET Core 8

using Microsoft.AspNetCore.Mvc;
using PrivacyIdeaServer.Lib;
using PrivacyIdeaServer.Models;
using System.Text.RegularExpressions;

namespace PrivacyIdeaServer.Controllers
{
    /// <summary>
    /// This endpoint is used to create, update, list and delete 
    /// privacyIDEA server definitions. privacyIDEA server definitions can be used for 
    /// Remote-Tokens and for Federation-Events.
    /// Port of Python's privacyideaserver_blueprint
    /// </summary>
    [ApiController]
    [Route("privacyideaserver")]
    public partial class PrivacyIDEAServerController : ControllerBase
    {
        private readonly IPrivacyIDEAServerService _serverService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PrivacyIDEAServerController> _logger;

        // Use source-generated regex for better performance
        [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
        private static partial Regex IdentifierValidationRegex();

        public PrivacyIDEAServerController(
            IPrivacyIDEAServerService serverService,
            IHttpClientFactory httpClientFactory,
            ILogger<PrivacyIDEAServerController> logger)
        {
            _serverService = serverService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// This call creates or updates a privacyIDEA Server definition
        /// </summary>
        /// <param name="identifier">The unique name of the privacyIDEA server definition</param>
        /// <param name="request">Request data containing url, tls, description</param>
        /// <returns>Success result</returns>
        [HttpPost("{identifier}")]
        public async Task<IActionResult> Create(
            string identifier,
            [FromBody] CreateServerRequest request)
        {
            try
            {
                // Validate and sanitize identifier
                identifier = identifier.Replace(" ", "_");
                
                // Ensure identifier only contains safe characters (alphanumeric, dash, underscore)
                if (!IdentifierValidationRegex().IsMatch(identifier))
                {
                    return BadRequest(new
                    {
                        result = new { status = false, value = false },
                        detail = new { message = "Identifier can only contain letters, numbers, dashes, and underscores" }
                    });
                }
                
                if (string.IsNullOrEmpty(request.Url))
                {
                    return BadRequest(new { result = new { status = false, value = false }, detail = new { message = "URL is required" } });
                }

                var id = await _serverService.AddPrivacyIDEAServerAsync(
                    identifier,
                    request.Url,
                    request.Tls,
                    request.Description ?? string.Empty
                );

                _logger.LogInformation($"Created/Updated privacyIDEA server '{identifier}' with id {id}");

                return Ok(new
                {
                    result = new { status = true, value = id > 0 },
                    detail = new { id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating/updating server '{identifier}'");
                return StatusCode(500, new
                {
                    result = new { status = false, value = false },
                    detail = new { message = ex.Message }
                });
            }
        }

        /// <summary>
        /// This call gets the list of privacyIDEA server definitions
        /// </summary>
        /// <returns>List of server definitions</returns>
        [HttpGet("")]
        public async Task<IActionResult> List()
        {
            try
            {
                var servers = await _serverService.ListPrivacyIDEAServersAsync();
                
                _logger.LogDebug($"Retrieved {servers.Count} privacyIDEA servers");

                return Ok(new
                {
                    result = new { status = true, value = servers }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing servers");
                return StatusCode(500, new
                {
                    result = new { status = false, value = new Dictionary<string, object>() },
                    detail = new { message = ex.Message }
                });
            }
        }

        /// <summary>
        /// This call deletes the specified privacyIDEA server configuration
        /// </summary>
        /// <param name="identifier">The unique name of the privacyIDEA server definition</param>
        /// <returns>Success result</returns>
        [HttpDelete("{identifier}")]
        public async Task<IActionResult> Delete(string identifier)
        {
            try
            {
                var id = await _serverService.DeletePrivacyIDEAServerAsync(identifier);
                
                _logger.LogInformation($"Deleted privacyIDEA server '{identifier}' with id {id}");

                return Ok(new
                {
                    result = new { status = true, value = id > 0 },
                    detail = new { id }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Server '{identifier}' not found: {ex.Message}");
                return NotFound(new
                {
                    result = new { status = false, value = false },
                    detail = new { message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting server '{identifier}'");
                return StatusCode(500, new
                {
                    result = new { status = false, value = false },
                    detail = new { message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Test the privacyIDEA server definition
        /// </summary>
        /// <param name="request">Test request data</param>
        /// <returns>Test result</returns>
        [HttpPost("test_request")]
        public async Task<IActionResult> TestRequest([FromBody] TestServerRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Identifier) ||
                    string.IsNullOrEmpty(request.Url) ||
                    string.IsNullOrEmpty(request.Username) ||
                    string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new
                    {
                        result = new { status = false, value = false },
                        detail = new { message = "identifier, url, username, and password are required" }
                    });
                }

                var serverConfig = new PrivacyIDEAServerDB
                {
                    Identifier = request.Identifier,
                    Url = request.Url,
                    Tls = request.Tls
                };

                var result = await PrivacyIDEAServer.RequestAsync(
                    serverConfig,
                    request.Username,
                    request.Password,
                    _httpClientFactory,
                    _logger
                );

                _logger.LogInformation($"Test request to server '{request.Identifier}' returned: {result}");

                return Ok(new
                {
                    result = new { status = true, value = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing server");
                return StatusCode(500, new
                {
                    result = new { status = false, value = false },
                    detail = new { message = ex.Message }
                });
            }
        }
    }

    /// <summary>
    /// Request model for creating/updating server
    /// </summary>
    public class CreateServerRequest
    {
        public string Url { get; set; } = string.Empty;
        public bool Tls { get; set; } = true;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request model for testing server
    /// </summary>
    public class TestServerRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Tls { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
