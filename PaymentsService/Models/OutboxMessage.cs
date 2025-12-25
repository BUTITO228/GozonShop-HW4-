using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public string Payload { get; set; }
        public bool Sent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
