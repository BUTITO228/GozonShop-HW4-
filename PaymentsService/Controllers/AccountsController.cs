using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PaymentsService.Services;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromHeader(Name = "X-User-Id")] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID required");
            var account = await _accountService.CreateAccountAsync(userId);
            return Ok(new { accountId = account.Id, userId = account.UserId, balance = account.Balance });
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit(
            [FromHeader(Name = "X-User-Id")] string userId,
            [FromBody] DepositRequest request)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID required");
            var success = await _accountService.DepositAsync(userId, request.Amount);
            if (!success) return BadRequest("Deposit failed");
            var balance = await _accountService.GetBalanceAsync(userId);
            return Ok(new { balance });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromHeader(Name = "X-User-Id")] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID required");
            var balance = await _accountService.GetBalanceAsync(userId);
            return Ok(new { balance });
        }
    }

    public class DepositRequest
    {
        public decimal Amount { get; set; }
    }
}
