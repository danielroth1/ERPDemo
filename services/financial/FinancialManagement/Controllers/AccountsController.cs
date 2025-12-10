using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;

namespace FinancialManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var account = await _accountService.CreateAccountAsync(request);
            return CreatedAtAction(nameof(GetAccountById), new { id = account.Id }, ApiResponse<AccountResponse>.SuccessResponse(account, "Account created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AccountResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetAccountById(string id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null)
        {
            return NotFound(ApiResponse<AccountResponse>.ErrorResponse("Account not found"));
        }
        return Ok(ApiResponse<AccountResponse>.SuccessResponse(account));
    }

    [HttpGet("number/{accountNumber}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetAccountByNumber(string accountNumber)
    {
        var account = await _accountService.GetAccountByNumberAsync(accountNumber);
        if (account == null)
        {
            return NotFound(ApiResponse<AccountResponse>.ErrorResponse("Account not found"));
        }
        return Ok(ApiResponse<AccountResponse>.SuccessResponse(account));
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetAccountByUserId(string userId)
    {
        var account = await _accountService.GetAccountByUserIdAsync(userId);
        if (account == null)
        {
            return NotFound(ApiResponse<AccountResponse>.ErrorResponse("No account found for this user"));
        }
        return Ok(ApiResponse<AccountResponse>.SuccessResponse(account));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountResponse>>>> GetAllAccounts(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var accounts = await _accountService.GetAllAccountsAsync(skip, limit);
        return Ok(ApiResponse<List<AccountResponse>>.SuccessResponse(accounts));
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<ApiResponse<List<AccountResponse>>>> GetAccountsByType(
        string type,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        if (!Enum.TryParse<AccountType>(type, true, out var accountType))
        {
            return BadRequest(ApiResponse<List<AccountResponse>>.ErrorResponse("Invalid account type"));
        }

        var accounts = await _accountService.GetAccountsByTypeAsync(accountType, skip, limit);
        return Ok(ApiResponse<List<AccountResponse>>.SuccessResponse(accounts));
    }

    [HttpGet("{id}/balance")]
    public async Task<ActionResult<ApiResponse<object>>> GetAccountBalance(string id)
    {
        var balance = await _accountService.GetAccountBalanceAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(new { accountId = id, balance }));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> UpdateAccount(
        string id,
        [FromBody] UpdateAccountRequest request)
    {
        var account = await _accountService.UpdateAccountAsync(id, request);
        if (account == null)
        {
            return NotFound(ApiResponse<AccountResponse>.ErrorResponse("Account not found"));
        }
        return Ok(ApiResponse<AccountResponse>.SuccessResponse(account, "Account updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAccount(string id)
    {
        try
        {
            var deleted = await _accountService.DeleteAccountAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Account not found"));
            }
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Account deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Adjust account balance by a fixed amount (Admin only)
    /// </summary>
    [HttpPost("{id}/adjust-balance")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> AdjustAccountBalance(
        string id,
        [FromQuery] decimal amount)
    {
        if (amount == 0)
        {
            return BadRequest(ApiResponse<AccountResponse>.ErrorResponse("Amount cannot be zero"));
        }

        var oldBalance = await _accountService.GetAccountBalanceAsync(id);
        var account = await _accountService.AdjustBalanceAsync(id, amount);
        
        if (account == null)
        {
            return NotFound(ApiResponse<AccountResponse>.ErrorResponse("Account not found"));
        }

        var userName = User.Identity?.Name ?? "System";
        _logger.LogInformation(
            "Admin {User} adjusted account {AccountNumber} balance from {OldBalance} to {NewBalance}",
            userName, account.AccountNumber, oldBalance, account.Balance);

        return Ok(ApiResponse<AccountResponse>.SuccessResponse(account, 
            $"Balance adjusted by {amount:C}. New balance: {account.Balance:C}"));
    }
}

