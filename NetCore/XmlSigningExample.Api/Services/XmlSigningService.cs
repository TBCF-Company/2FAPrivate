// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using XmlSigningExample.Api.Models;

namespace XmlSigningExample.Api.Services;

/// <summary>
/// Service for XML signing with 2FA authentication
/// </summary>
public interface IXmlSigningService
{
    /// <summary>
    /// Initiates XML signing process and generates 2FA code
    /// </summary>
    Task<AuthCodeResponse> InitiateSigningAsync(XmlSigningRequest request);
    
    /// <summary>
    /// Verifies 2FA code and completes XML signing
    /// </summary>
    Task<SignedXmlResponse> VerifyAndSignAsync(VerifyAndSignRequest request);
}

/// <summary>
/// Implementation of XML signing service with 2FA
/// </summary>
public class XmlSigningService : IXmlSigningService
{
    private readonly ILogger<XmlSigningService> _logger;
    
    // In-memory storage for pending signing sessions
    // In production, use a database or distributed cache
    private readonly ConcurrentDictionary<string, SigningSession> _pendingSessions = new();
    
    // Track failed verification attempts per session (for rate limiting)
    private readonly ConcurrentDictionary<string, int> _failedAttempts = new();
    
    // Session expiry time (5 minutes)
    private const int SessionExpiryMinutes = 5;
    
    // Maximum failed attempts before locking session
    private const int MaxFailedAttempts = 3;
    
    public XmlSigningService(ILogger<XmlSigningService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Start background cleanup task to remove expired sessions
        Task.Run(() => CleanupExpiredSessionsAsync());
    }
    
    /// <summary>
    /// Background task to periodically clean up expired sessions
    /// </summary>
    private async Task CleanupExpiredSessionsAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1)); // Run cleanup every minute
                
                var expiredSessions = _pendingSessions
                    .Where(kvp => DateTime.UtcNow > kvp.Value.ExpiresAt)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var sessionId in expiredSessions)
                {
                    _pendingSessions.TryRemove(sessionId, out _);
                    _failedAttempts.TryRemove(sessionId, out _);
                    _logger.LogDebug("Cleaned up expired session {SessionId}", sessionId);
                }
                
                if (expiredSessions.Any())
                {
                    _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }
    }
    
    public Task<AuthCodeResponse> InitiateSigningAsync(XmlSigningRequest request)
    {
        try
        {
            _logger.LogInformation("Initiating XML signing for user {Username}", request.Username);
            
            // Validate XML content
            if (string.IsNullOrWhiteSpace(request.XmlContent))
            {
                return Task.FromResult(new AuthCodeResponse
                {
                    Success = false,
                    Message = "XML content is required"
                });
            }
            
            // Validate that it's valid XML
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(request.XmlContent);
            }
            catch (XmlException ex)
            {
                _logger.LogWarning("Invalid XML content: {Message}", ex.Message);
                return Task.FromResult(new AuthCodeResponse
                {
                    Success = false,
                    Message = $"Invalid XML content: {ex.Message}"
                });
            }
            
            // Generate a 2-character authentication code (00-99)
            var authCode = GenerateTwoCharacterCode();
            
            // Create a session ID
            var sessionId = Guid.NewGuid().ToString();
            
            // Store the session
            var session = new SigningSession
            {
                SessionId = sessionId,
                AuthCode = authCode,
                XmlContent = request.XmlContent,
                Username = request.Username,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(SessionExpiryMinutes)
            };
            
            _pendingSessions[sessionId] = session;
            
            _logger.LogInformation("Generated 2FA code for session {SessionId}: {AuthCode}", 
                sessionId, authCode);
            
            return Task.FromResult(new AuthCodeResponse
            {
                Success = true,
                AuthCode = authCode,
                SessionId = sessionId,
                Message = "Please enter this 2-character code in your authenticator app to complete signing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating XML signing");
            return Task.FromResult(new AuthCodeResponse
            {
                Success = false,
                Message = $"Error initiating signing: {ex.Message}"
            });
        }
    }
    
    public Task<SignedXmlResponse> VerifyAndSignAsync(VerifyAndSignRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying auth code for session {SessionId}", request.SessionId);
            
            // Check if session exists
            if (!_pendingSessions.TryGetValue(request.SessionId, out var session))
            {
                _logger.LogWarning("Session {SessionId} not found", request.SessionId);
                return Task.FromResult(new SignedXmlResponse
                {
                    Success = false,
                    Message = "Session not found. Please initiate signing again."
                });
            }
            
            // Check if session has expired
            if (DateTime.UtcNow > session.ExpiresAt)
            {
                _pendingSessions.TryRemove(request.SessionId, out _);
                _logger.LogWarning("Session {SessionId} has expired", request.SessionId);
                return Task.FromResult(new SignedXmlResponse
                {
                    Success = false,
                    Message = "Session has expired. Please initiate signing again."
                });
            }
            
            // Check if session is locked due to too many failed attempts
            if (_failedAttempts.TryGetValue(request.SessionId, out var attempts) && attempts >= MaxFailedAttempts)
            {
                _pendingSessions.TryRemove(request.SessionId, out _);
                _failedAttempts.TryRemove(request.SessionId, out _);
                _logger.LogWarning("Session {SessionId} locked due to too many failed attempts", request.SessionId);
                return Task.FromResult(new SignedXmlResponse
                {
                    Success = false,
                    Message = "Too many failed attempts. Please initiate signing again."
                });
            }
            
            // Verify auth code
            if (request.AuthCode != session.AuthCode)
            {
                // Increment failed attempts
                _failedAttempts.AddOrUpdate(request.SessionId, 1, (key, count) => count + 1);
                var currentAttempts = _failedAttempts[request.SessionId];
                var remaining = MaxFailedAttempts - currentAttempts;
                
                _logger.LogWarning("Invalid auth code for session {SessionId}. Attempts: {Attempts}/{Max}", 
                    request.SessionId, currentAttempts, MaxFailedAttempts);
                
                return Task.FromResult(new SignedXmlResponse
                {
                    Success = false,
                    Message = remaining > 0 
                        ? $"Invalid authentication code. {remaining} attempt(s) remaining."
                        : "Too many failed attempts. Session locked."
                });
            }
            
            // Auth code is valid, proceed with XML signing
            var signedXml = SignXmlDocument(session.XmlContent);
            
            // Remove the session and failed attempts counter
            _pendingSessions.TryRemove(request.SessionId, out _);
            _failedAttempts.TryRemove(request.SessionId, out _);
            
            _logger.LogInformation("Successfully signed XML for session {SessionId}", request.SessionId);
            
            return Task.FromResult(new SignedXmlResponse
            {
                Success = true,
                SignedXml = signedXml,
                Message = "XML document signed successfully",
                SignedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying and signing XML");
            return Task.FromResult(new SignedXmlResponse
            {
                Success = false,
                Message = $"Error signing XML: {ex.Message}"
            });
        }
    }
    
    /// <summary>
    /// Generates a random 2-character code (00-99)
    /// </summary>
    private static string GenerateTwoCharacterCode()
    {
        var random = RandomNumberGenerator.GetInt32(0, 100);
        return random.ToString("D2"); // Format as 2 digits with leading zero
    }
    
    /// <summary>
    /// Signs an XML document using RSA
    /// </summary>
    private string SignXmlDocument(string xmlContent)
    {
        try
        {
            // Load the XML document
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlContent);
            
            // Create a new RSA key for signing (in production, use a proper certificate)
            using var rsa = RSA.Create(2048);
            
            // Create the SignedXml object
            var signedXml = new SignedXml(doc)
            {
                SigningKey = rsa
            };
            
            // Create a reference to be signed (whole document)
            var reference = new Reference
            {
                Uri = ""
            };
            
            // Add an enveloped transformation to the reference
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            
            // Add the reference to the SignedXml object
            signedXml.AddReference(reference);
            
            // Compute the signature
            signedXml.ComputeSignature();
            
            // Get the XML representation of the signature
            var xmlDigitalSignature = signedXml.GetXml();
            
            // Append the signature to the XML document
            doc.DocumentElement?.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
            
            // Return the signed XML as string
            using var stringWriter = new StringWriter();
            using var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            });
            doc.Save(xmlTextWriter);
            
            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing XML document");
            throw;
        }
    }
}

/// <summary>
/// Internal model for tracking signing sessions
/// </summary>
internal class SigningSession
{
    public string SessionId { get; set; } = string.Empty;
    public string AuthCode { get; set; } = string.Empty;
    public string XmlContent { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
