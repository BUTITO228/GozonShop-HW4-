using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    public enum TransactionType
    {
        Deposit,
        Withdrawal
    }

    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; }
        public Guid? OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
