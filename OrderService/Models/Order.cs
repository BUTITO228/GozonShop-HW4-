using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public enum OrderStatus { NEW, FINISHED, CANCELLED }

    public class Order
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
