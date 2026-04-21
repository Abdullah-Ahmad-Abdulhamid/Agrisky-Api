using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.Hubs;
using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepo _repo;
    private readonly IOwnerRepo _ownerRepo;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderRepo repo,
        IOwnerRepo ownerRepo,
        IHubContext<AppHub> hubContext,
        ILogger<OrderController> logger)
    {
        _repo = repo;
        _ownerRepo = ownerRepo;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ── POST /api/order — authenticated users only ────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromForm] CreateOrderFormInput formInput,
        IFormFile? proof)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // ── 1. ALWAYS read UserId from the JWT — never from the request body ──
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var authenticatedUserId))
            return Unauthorized(new { message = "Invalid token." });

        // ── 2. Deserialize and validate items ─────────────────────────────────
        List<OrderItemInputDto>? items;
        ShippingDto? shipping;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            items = JsonSerializer.Deserialize<List<OrderItemInputDto>>(formInput.Items, options);
            shipping = JsonSerializer.Deserialize<ShippingDto>(formInput.Shipping, options);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { message = "Invalid JSON format.", detail = ex.Message });
        }

        if (items == null || items.Count == 0 || items.Count > 100)
            return BadRequest(new { message = "Order must contain between 1 and 100 items." });

        if (shipping == null)
            return BadRequest(new { message = "Shipping information is invalid." });

        // ── 3. Validate deserialized objects via data annotations ─────────────
        var itemErrors = new List<string>();
        foreach (var item in items)
        {
            var ctx = new ValidationContext(item);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(item, ctx, results, true))
                itemErrors.AddRange(results.Select(r => r.ErrorMessage ?? "Invalid item."));
        }
        if (itemErrors.Count > 0)
            return BadRequest(new { message = "Invalid items.", errors = itemErrors });

        var shippingCtx = new ValidationContext(shipping);
        var shippingResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(shipping, shippingCtx, shippingResults, true))
            return BadRequest(new
            {
                message = "Invalid shipping data.",
                errors = shippingResults.Select(r => r.ErrorMessage)
            });

        // ── 4. Build internal DTO — UserId is server-set ──────────────────────
        var dto = new CreateOrderDto
        {
            UserId = authenticatedUserId,   // ← JWT-sourced
            PaymentMethod = formInput.PaymentMethod,
            VodafoneCashTransactionId = formInput.VodafoneCashTransactionId,
            Items = items,
            Shipping = shipping
        };

        try
        {
            var order = await _repo.CreateOrder(dto, proof);

            _logger.LogInformation(
                "Order {OrderId} created by user {UserId} via {Method}",
                order.OrderID, authenticatedUserId, dto.PaymentMethod);

            // Notify admins via SignalR
            await _hubContext.Clients.Group("Admins")
                .SendAsync("NewOrderReceived", new
                {
                    orderId = order.OrderID,
                    userId = authenticatedUserId,
                    total = order.TotalAmount
                });

            return Ok(new { message = "Order created successfully.", orderId = order.OrderID });
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule violations (out of stock, invalid method, etc.)
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── PATCH /api/order/{id}/status — Admin only ─────────────────────────────
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { message = $"Invalid status value: '{request.Status}'." });

        var order = await _repo.GetById(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (!IsValidStatusTransition(order.Status, newStatus))
            return BadRequest(new
            {
                message = $"Cannot transition from '{order.Status}' to '{newStatus}'."
            });

        var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        var previousStatus = order.Status;
        order.Status = newStatus.ToString();
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = adminEmail;

        _repo.Update(order);
        await _repo.SaveAsync();

        _logger.LogInformation(
            "Admin {Admin} changed order {OrderId} status from {Old} to {New} at {Time}",
            adminEmail, id, previousStatus, newStatus, DateTime.UtcNow);

        // Notify the specific user about their order update
        await _hubContext.Clients.Group($"User_{order.UserID}")
            .SendAsync("OrderStatusUpdated", new { orderId = id, newStatus = newStatus.ToString() });

        return Ok(new { message = "Status updated successfully." });
    }

    // ── POST /api/order/{id}/verify-payment — Admin only ─────────────────────
    [HttpPost("{id}/verify-payment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifyPayment(
        int id,
        [FromBody] VerifyPaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        var result = await _repo.VerifyPayment(
            id, request.Approve, request.AdminNote, adminEmail);

        if (!result)
            return BadRequest(new
            {
                message = "Payment verification failed. Order may not exist or " +
                          "payment is not in 'PendingVerification' state."
            });

        _logger.LogInformation(
            "Admin {Admin} {Action} payment for order {OrderId}. Note: {Note}",
            adminEmail,
            request.Approve ? "approved" : "rejected",
            id,
            request.AdminNote ?? "(none)");

        return Ok(new
        {
            message = request.Approve
                ? "Payment approved. Order moved to Processing."
                : "Payment rejected."
        });
    }

    // ── State machine: only these transitions are legal ───────────────────────
    private static bool IsValidStatusTransition(string currentStatus, OrderStatus newStatus)
    {
        var transitions = new Dictionary<string, List<OrderStatus>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Pending",    new() { OrderStatus.Processing, OrderStatus.Cancelled } },
            { "Processing", new() { OrderStatus.Shipped,    OrderStatus.Cancelled } },
            { "Shipped",    new() { OrderStatus.Delivered,  OrderStatus.Returned  } },
            { "Delivered",  new() { OrderStatus.Returned  } },
            { "Cancelled",  new() { OrderStatus.Pending   } },   // allow re-open
            { "Returned",   new() { OrderStatus.Refunded  } }
        };

        return transitions.TryGetValue(currentStatus, out var allowed)
               && allowed.Contains(newStatus);
    }
}

// ── Order status enum ─────────────────────────────────────────────────────────
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Returned,
    Refunded
}