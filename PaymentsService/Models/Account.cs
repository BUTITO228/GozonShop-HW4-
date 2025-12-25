using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
