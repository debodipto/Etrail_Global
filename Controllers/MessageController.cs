using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EtrailGlobal.Database;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessageController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int senderId))
            return Unauthorized();

        if (senderId == request.ReceiverId)
            return BadRequest(new { message = "You cannot send a message to yourself" });

        var receiver = await _context.Users.FindAsync(request.ReceiverId);
        if (receiver == null)
            return NotFound(new { message = "Receiver not found" });

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Content = request.Content,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.Content,
            message.Timestamp,
            message.IsRead
        });
    }

    [HttpGet("chat/{otherUserId}")]
    public async Task<IActionResult> GetChatHistory(int otherUserId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
            return Unauthorized();

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => 
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead);
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }
        if (unreadMessages.Any())
        {
            await _context.SaveChangesAsync();
        }

        var result = messages.Select(m => new
        {
            m.Id,
            m.SenderId,
            SenderName = m.Sender?.Username ?? "Unknown",
            m.ReceiverId,
            ReceiverName = m.Receiver?.Username ?? "Unknown",
            m.Content,
            m.Timestamp,
            m.IsRead
        });

        return Ok(result);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentChats()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
            return Unauthorized();

        var senderIds = await _context.Messages
            .Where(m => m.ReceiverId == currentUserId)
            .Select(m => m.SenderId)
            .Distinct()
            .ToListAsync();

        var receiverIds = await _context.Messages
            .Where(m => m.SenderId == currentUserId)
            .Select(m => m.ReceiverId)
            .Distinct()
            .ToListAsync();

        var connectedUserIds = senderIds.Union(receiverIds).ToList();

        var recentChats = new List<object>();

        foreach (var userId in connectedUserIds)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) continue;

            var lastMessage = await _context.Messages
                .Where(m => 
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            if (lastMessage == null) continue;

            recentChats.Add(new
            {
                UserId = user.Id,
                Username = user.Username,
                CompanyName = user.CompanyName,
                Role = user.Role.ToString(),
                LastMessageContent = lastMessage.Content,
                LastMessageTime = lastMessage.Timestamp,
                UnreadCount = await _context.Messages.CountAsync(m => m.SenderId == userId && m.ReceiverId == currentUserId && !m.IsRead)
            });
        }

        var sortedChats = recentChats
            .Cast<dynamic>()
            .OrderByDescending(c => (DateTime)c.LastMessageTime)
            .ToList();

        return Ok(sortedChats);
    }
}

public class SendMessageRequest
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}
