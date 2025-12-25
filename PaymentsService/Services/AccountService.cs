using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;

namespace PaymentsService.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(string userId);
        Task<Account> GetAccountByUserIdAsync(string userId);
        Task<bool> DepositAsync(string userId, decimal amount);
        Task<decimal> GetBalanceAsync(string userId);
    }

    public class AccountService : IAccountService
    {
        private readonly PaymentsDbContext _context;

        public AccountService(PaymentsDbContext context)
        {
            _context = context;
        }

        public async Task<Account> CreateAccountAsync(string userId)
        {
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);
            if (existingAccount != null) return existingAccount;

            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account> GetAccountByUserIdAsync(string userId)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<bool> DepositAsync(string userId, decimal amount)
        {
            if (amount <= 0) return false;
            var account = await GetAccountByUserIdAsync(userId);
            if (account == null) return false;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = "Deposit",
                CreatedAt = DateTime.UtcNow
            };
            account.Balance += amount;
            account.UpdatedAt = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetBalanceAsync(string userId)
        {
            var account = await GetAccountByUserIdAsync(userId);
            return account?.Balance ?? 0;
        }
    }
}
