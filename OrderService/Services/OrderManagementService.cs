using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services
{
    public interface IOrderManagementService
    {
        Task<Order> CreateOrderAsync(string userId, decimal amount, string description);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetOrderByIdAsync(Guid orderId);
        Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
    }

    public class OrderManagementService : IOrderManagementService
    {
        private readonly OrderDbContext _context;

        public OrderManagementService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(string userId, decimal amount, string description)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = amount,
                    Description = description,
                    Status = OrderStatus.NEW,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                var paymentRequest = new PaymentRequestMessage
                {
                    OrderId = order.Id,
                    UserId = userId,
                    Amount = amount,
                    MessageId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Payload = JsonSerializer.Serialize(paymentRequest),
                    Sent = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OutboxMessages.Add(outboxMessage);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"Order {order.Id} created with outbox message");

                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error creating order: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders.FindAsync(orderId);
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                Console.WriteLine($"Order {orderId} status updated to {status}");
            }
        }
    }
}
