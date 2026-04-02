using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Subscription management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class SubscriptionController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(IUnitOfWork unitOfWork, ILogger<SubscriptionController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get subscription information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var subscriptions = await _unitOfWork.Query<Subscription>().ToListAsync();
        
        return Ok(new
        {
            result = new
            {
                value = subscriptions.Select(s => new
                {
                    s.Id,
                    s.Application,
                    for_name = s.ForName,
                    for_email = s.ForEmail,
                    for_address = s.ForAddress,
                    for_phone = s.ForPhone,
                    by_name = s.ByName,
                    by_email = s.ByEmail,
                    date_from = s.DateFrom,
                    date_till = s.DateTill,
                    num_users = s.NumUsers,
                    num_tokens = s.NumTokens,
                    num_clients = s.NumClients,
                    level = s.Level
                })
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Upload/create a subscription
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubscriptionRequest request)
    {
        var subscription = new Subscription
        {
            Application = request.Application ?? "privacyidea",
            ForName = request.ForName,
            ForEmail = request.ForEmail,
            ForAddress = request.ForAddress,
            ForPhone = request.ForPhone,
            ForUrl = request.ForUrl,
            ForComment = request.ForComment,
            ByName = request.ByName,
            ByEmail = request.ByEmail,
            ByAddress = request.ByAddress,
            ByPhone = request.ByPhone,
            ByUrl = request.ByUrl,
            DateFrom = request.DateFrom,
            DateTill = request.DateTill,
            NumUsers = request.NumUsers,
            NumTokens = request.NumTokens,
            NumClients = request.NumClients,
            Level = request.Level,
            Signature = request.Signature
        };

        _unitOfWork.Add(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Subscription created for {Name}", request.ForName);

        return Ok(new
        {
            result = new { status = true, value = subscription.Id },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a subscription
    /// </summary>
    [HttpDelete]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int? id = null)
    {
        var query = _unitOfWork.Query<Subscription>();
        
        Subscription? subscription;
        if (id.HasValue)
        {
            subscription = await query.FirstOrDefaultAsync(s => s.Id == id.Value);
        }
        else
        {
            subscription = await query.FirstOrDefaultAsync();
        }

        if (subscription == null)
            return NotFound(new { result = new { status = false }, detail = "Subscription not found" });

        _unitOfWork.Delete(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Subscription deleted: {Id}", subscription.Id);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }
}

public class SubscriptionRequest
{
    public string? Application { get; set; }
    public string? ForName { get; set; }
    public string? ForEmail { get; set; }
    public string? ForAddress { get; set; }
    public string? ForPhone { get; set; }
    public string? ForUrl { get; set; }
    public string? ForComment { get; set; }
    public string? ByName { get; set; }
    public string? ByEmail { get; set; }
    public string? ByAddress { get; set; }
    public string? ByPhone { get; set; }
    public string? ByUrl { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTill { get; set; }
    public int? NumUsers { get; set; }
    public int? NumTokens { get; set; }
    public int? NumClients { get; set; }
    public string? Level { get; set; }
    public string? Signature { get; set; }
}
