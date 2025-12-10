import { financialApiClient } from './financial-api.client';
import type { Account, Transaction, PaginatedResponse, TransactionType } from '../types';
import type { AccountResponse, TransactionResponse } from '../generated/clients/financial/models';

// Map Kiota types to legacy types
function mapAccountResponse(account: AccountResponse): Account {
  return {
    id: account.id || '',
    name: account.name || '',
    accountNumber: account.accountNumber || '',
    type: (account.type || '') as any,
    balance: account.balance || 0,
    isActive: account.isActive || false,
    createdAt: account.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: account.updatedAt?.toISOString() || new Date().toISOString(),
  };
}

function mapTransactionResponse(transaction: TransactionResponse): Transaction {
  // Calculate total from entries (sum of debits or credits)
  const amount = transaction.entries?.reduce((sum, entry) => 
    sum + ((entry.debit ?? 0) + (entry.credit ?? 0)), 0
  ) ?? 0;
  
  return {
    id: transaction.id || '',
    transactionNumber: transaction.transactionNumber || '',
    date: transaction.date?.toISOString() || new Date().toISOString(),
    description: transaction.description || '',
    amount,
    type: (transaction.type || 'Expense') as TransactionType,
    debitAccountId: transaction.entries?.[0]?.accountId ?? '',
    creditAccountId: transaction.entries?.[1]?.accountId ?? '',
    reference: transaction.referenceId ?? '',
    createdAt: transaction.createdAt?.toISOString() || new Date().toISOString(),
  };
}

class FinancialService {
  // Accounts
  async getAccounts(): Promise<Account[]> {
    const accounts = await financialApiClient.getAccounts();
    return accounts.map(mapAccountResponse);
  }

  async getAccountById(accountId: string): Promise<Account> {
    const account = await financialApiClient.getAccountById(accountId);
    return mapAccountResponse(account);
  }

  async getMainAccount(): Promise<Account | null> {
    // Get first active account as main account
    // In a real app, you'd have a specific "main" or "cash" account
    const accounts = await this.getAccounts();
    const activeAccount = accounts.find(a => a.isActive);
    return activeAccount || null;
  }

  async createAccount(account: Partial<Account>): Promise<Account> {
    const response = await financialApiClient.createAccount({
      name: account.name || '',
      type: account.type || '',
      category: 'CurrentAssets',
      currency: 'USD',
      description: '',
    });
    return mapAccountResponse(response);
  }

  async adjustAccountBalance(accountId: string, amount: number): Promise<Account> {
    const response = await financialApiClient.adjustAccountBalance(accountId, amount);
    return mapAccountResponse(response);
  }

  // Transactions
  async getTransactions(page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<Transaction>> {
    const skip = (page - 1) * pageSize;
    const transactions = await financialApiClient.getTransactions(skip, pageSize);
    
    return {
      items: transactions.map(mapTransactionResponse),
      page: page,
      pageSize: pageSize,
      totalCount: transactions.length,
      totalPages: 1,
    };
  }

  async createTransaction(transaction: Partial<Transaction>): Promise<Transaction> {
    const response = await financialApiClient.createTransaction({
      date: transaction.date ? new Date(transaction.date) : new Date(),
      description: transaction.description || '',
      type: transaction.type || 'Expense',
      entries: [],
    });
    return mapTransactionResponse(response);
  }

  async deleteTransaction(id: string): Promise<void> {
    await financialApiClient.deleteTransaction(id);
  }

  // User Account Management
  async getUserAccounts(userId: string): Promise<Account[]> {
    // Get user's account using the dedicated endpoint
    const account = await financialApiClient.getUserAccount(userId);
    return account ? [mapAccountResponse(account)] : [];
  }

  async createUserAccount(userId: string, userName: string): Promise<Account> {
    const accountName = `${userName} - Personal Account`;
    const response = await financialApiClient.createAccount({
      name: accountName,
      type: 'Asset',
      category: 'CurrentAssets',
      currency: 'USD',
      userId: userId,
      description: `Personal account for user ${userId}`,
    });
    return mapAccountResponse(response);
  }

  async deleteUserAccount(accountId: string): Promise<void> {
    // Deactivate instead of delete
    const account = await this.getAccountById(accountId);
    await financialApiClient.updateAccount(accountId, {
      name: account.name,
      description: account.accountNumber,
      isActive: false,
    });
  }

  // Reports
  async getBalanceSheet(): Promise<any> {
    const report = await financialApiClient.getBalanceSheet();
    return report;
  }

  async getProfitLoss(startDate: string, endDate: string): Promise<any> {
    const report = await financialApiClient.getIncomeStatement(
      new Date(startDate),
      new Date(endDate)
    );
    return report;
  }
}

export const financialService = new FinancialService();
export default financialService;
