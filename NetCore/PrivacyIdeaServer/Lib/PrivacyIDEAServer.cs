// SPDX-License-Identifier: AGPL-3.0-or-later
// 2017-08-24 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//           Remote privacyIDEA server
// Ported to .NET Core 8

using System.Net.Http;
using System.Text.Json;
using System.Web;
using PrivacyIdeaServer.Models;

namespace PrivacyIdeaServer.Lib
{
    /// <summary>
    /// PrivacyIDEA Server object with configuration and test functionality
    /// This is the library for creating, listing and deleting remote privacyIDEA 
    /// server objects in the Database.
    /// Port of Python's PrivacyIDEAServer class
    /// </summary>
    public class PrivacyIDEAServer
    {
        private readonly PrivacyIDEAServerDB _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PrivacyIDEAServer> _logger;

        public PrivacyIDEAServerDB Config => _config;

        public PrivacyIDEAServer(PrivacyIDEAServerDB config, IHttpClientFactory httpClientFactory, ILogger<PrivacyIDEAServer> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Perform an HTTP validate/check request to the remote privacyIDEA Server
        /// </summary>
        /// <param name="user">The username</param>
        /// <param name="password">The password</param>
        /// <param name="serial">The serial number of a token (optional)</param>
        /// <param name="realm">An optional realm, if not contained in username</param>
        /// <param name="transactionId">An optional transaction_id</param>
        /// <param name="resolver">An optional resolver</param>
        /// <returns>HTTP response message</returns>
        public async Task<HttpResponseMessage> ValidateCheckAsync(
            string? user = null,
            string? password = null,
            string? serial = null,
            string? realm = null,
            string? transactionId = null,
            string? resolver = null)
        {
            var data = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(password))
                data["pass"] = HttpUtility.UrlEncode(password);
            if (!string.IsNullOrEmpty(user))
                data["user"] = HttpUtility.UrlEncode(user);
            if (!string.IsNullOrEmpty(serial))
                data["serial"] = serial;
            if (!string.IsNullOrEmpty(realm))
                data["realm"] = realm;
            if (!string.IsNullOrEmpty(transactionId))
                data["transaction_id"] = transactionId;
            if (!string.IsNullOrEmpty(resolver))
                data["resolver"] = resolver;

            var client = _httpClientFactory.CreateClient();
            
            // Configure TLS/SSL validation
            if (!_config.Tls)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                client = new HttpClient(handler);
            }

            client.Timeout = TimeSpan.FromSeconds(60);

            var content = new FormUrlEncodedContent(data);
            var url = $"{_config.Url.TrimEnd('/')}/validate/check";
            
            var response = await client.PostAsync(url, content);
            
            return response;
        }

        /// <summary>
        /// Perform an HTTP test request to the privacyIDEA server
        /// </summary>
        /// <param name="config">The privacyIDEA configuration</param>
        /// <param name="user">The username to test</param>
        /// <param name="password">The password/OTP to test</param>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>True if successful, False otherwise</returns>
        public static async Task<bool> RequestAsync(
            PrivacyIDEAServerDB config,
            string user,
            string password,
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            try
            {
                var data = new Dictionary<string, string>
                {
                    ["user"] = HttpUtility.UrlEncode(user),
                    ["pass"] = HttpUtility.UrlEncode(password)
                };

                var client = httpClientFactory.CreateClient();
                
                // Configure TLS/SSL validation
                if (!config.Tls)
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                    client = new HttpClient(handler);
                }

                client.Timeout = TimeSpan.FromSeconds(60);

                var content = new FormUrlEncodedContent(data);
                var url = $"{config.Url.TrimEnd('/')}/validate/check";
                
                var response = await client.PostAsync(url, content);
                
                logger.LogDebug($"Sent request to privacyIDEA server. Status code returned: {response.StatusCode}");

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogWarning($"The request to the remote privacyIDEA server {config.Url} returned a status code: {response.StatusCode}");
                    return false;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                var result = jsonResponse.GetProperty("result");
                return result.GetProperty("status").GetBoolean() && result.GetProperty("value").GetBoolean();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error testing privacyIDEA server {config.Url}");
                return false;
            }
        }
    }
}
