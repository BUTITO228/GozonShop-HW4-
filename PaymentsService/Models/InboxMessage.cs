using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    public class InboxMessage
    {
        [Key]
        public Guid MessageId { get; set; }
        public string Payload { get; set; }
        public bool Processed { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
