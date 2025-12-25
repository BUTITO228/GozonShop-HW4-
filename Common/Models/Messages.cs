using System;

namespace Common.Models
{
    public class PaymentRequestMessage
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public Guid MessageId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentResultMessage
    {
        public Guid OrderId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid MessageId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
