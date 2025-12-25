using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderManagementService _orderService;

        public OrdersController(IOrderManagementService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromHeader(Name = "X-User-Id")] string userId,
            [FromBody] CreateOrderRequest request)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required");

            var order = await _orderService.CreateOrderAsync(userId, request.Amount, request.Description);

            return Ok(new
            {
                orderId = order.Id,
                userId = order.UserId,
                amount = order.Amount,
                description = order.Description,
                status = order.Status.ToString(),
                createdAt = order.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromHeader(Name = "X-User-Id")] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required");

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
                return NotFound();

            return Ok(new
            {
                orderId = order.Id,
                userId = order.UserId,
                amount = order.Amount,
                description = order.Description,
                status = order.Status.ToString(),
                createdAt = order.CreatedAt,
                updatedAt = order.UpdatedAt
            });
        }
    }

    public class CreateOrderRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
