using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;

namespace PaymentsService.Services
{
    public interface IPaymentProcessingService
    {
        Task ProcessPaymentRequestAsync(PaymentRequestMessage message);
    }

    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly PaymentsDbContext _context;

        public PaymentProcessingService(PaymentsDbContext context)
        {
            _context = context;
        }

        public async Task ProcessPaymentRequestAsync(PaymentRequestMessage message)
        {
            // Transactional Inbox - проверка идемпотентности
            var inboxMessage = await _context.InboxMessages
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

            if (inboxMessage != null && inboxMessage.Processed)
            {
                Console.WriteLine($"Message {message.MessageId} already processed");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (inboxMessage == null)
                {
                    inboxMessage = new InboxMessage
                    {
                        MessageId = message.MessageId,
                        Payload = JsonSerializer.Serialize(message),
                        Processed = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _context.InboxMessages.Add(inboxMessage);
                }

                var result = await ProcessPaymentAsync(message);

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Payload = JsonSerializer.Serialize(result),
                    Sent = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OutboxMessages.Add(outboxMessage);
                inboxMessage.Processed = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine($"Payment for order {message.OrderId} processed: {result.Success}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private async Task<PaymentResultMessage> ProcessPaymentAsync(PaymentRequestMessage request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == request.UserId);
            if (account == null)
            {
                return new PaymentResultMessage
                {
                    OrderId = request.OrderId,
                    Success = false,
                    Message = "Account not found",
                    MessageId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Проверка идемпотентности списания
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == request.OrderId);
            if (existingTransaction != null)
            {
                return new PaymentResultMessage
                {
                    OrderId = request.OrderId,
                    Success = true,
                    Message = "Payment already processed",
                    MessageId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
            }

            if (account.Balance < request.Amount)
            {
                return new PaymentResultMessage
                {
                    OrderId = request.OrderId,
                    Success = false,
                    Message = "Insufficient funds",
                    MessageId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
            }

            var withdrawalTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Amount = request.Amount,
                Type = TransactionType.Withdrawal,
                Description = $"Payment for order {request.OrderId}",
                OrderId = request.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            account.Balance -= request.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            _context.Transactions.Add(withdrawalTransaction);

            return new PaymentResultMessage
            {
                OrderId = request.OrderId,
                Success = true,
                Message = "Payment successful",
                MessageId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
