import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchAccounts, fetchTransactions, deleteTransaction } from './financialSlice';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { CurrencyDollarIcon, TrashIcon } from '@heroicons/react/24/outline';
import { AccountType, TransactionType } from '../../types';
import toast from 'react-hot-toast';

export const FinancialPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { accounts, transactions, isLoading, totalTransactions } = useAppSelector(
    (state) => state.financial
  );
  const pageSize = 10;

  useEffect(() => {
    dispatch(fetchAccounts());
    dispatch(fetchTransactions({ page: 1, pageSize }));
  }, [dispatch]);

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this transaction?')) {
      try {
        await dispatch(deleteTransaction(id)).unwrap();
        toast.success('Transaction deleted successfully');
      } catch (error: any) {
        toast.error(error || 'Failed to delete transaction');
      }
    }
  };

  const getTransactionTypeColor = (type: string) => {
    switch (type) {
      case TransactionType.Sale:
      case TransactionType.Receipt:
        return 'text-green-600';
      case TransactionType.Purchase:
      case TransactionType.Payment:
        return 'text-red-600';
      case TransactionType.Transfer:
        return 'text-blue-600';
      case TransactionType.Adjustment:
        return 'text-orange-600';
      default:
        return 'text-gray-600';
    }
  };

  const calculateAccountBalance = (accountType: string) => {
    const accountIds = accounts
      .filter((a) => a.type === accountType)
      .map((a) => a.id);

    return transactions
      .filter((t) => accountIds.includes(t.debitAccountId) || accountIds.includes(t.creditAccountId))
      .reduce((sum, t) => {
        if (t.type === TransactionType.Sale || t.type === TransactionType.Receipt) {
          return sum + t.amount;
        } else if (t.type === TransactionType.Purchase || t.type === TransactionType.Payment) {
          return sum - t.amount;
        }
        return sum;
      }, 0);
  };

  if (isLoading && transactions.length === 0) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Financial Management</h1>
      </div>

      {/* Account Balances */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <CurrencyDollarIcon className="h-10 w-10 text-green-500" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Total Assets</p>
              <p className="text-2xl font-bold text-green-600">
                ${calculateAccountBalance(AccountType.Asset).toFixed(2)}
              </p>
            </div>
          </div>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Total Liabilities</p>
          <p className="text-2xl font-bold text-red-600">
            ${calculateAccountBalance(AccountType.Liability).toFixed(2)}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Equity</p>
          <p className="text-2xl font-bold text-purple-600">
            ${calculateAccountBalance(AccountType.Equity).toFixed(2)}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Total Transactions</p>
          <p className="text-2xl font-bold text-gray-900">{totalTransactions}</p>
        </div>
      </div>

      {/* Transactions Table */}
      <div className="card">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Date
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Reference
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Description
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Amount
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {transactions.map((transaction) => (
                <tr key={transaction.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {new Date(transaction.date).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {transaction.reference}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-900">
                    {transaction.description}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`text-xs font-semibold ${getTransactionTypeColor(transaction.type)}`}>
                      {transaction.type}
                    </span>
                  </td>
                  <td className={`px-6 py-4 whitespace-nowrap text-sm font-semibold text-right ${getTransactionTypeColor(transaction.type)}`}>
                    {transaction.type === TransactionType.Purchase || transaction.type === TransactionType.Payment
                      ? `-$${transaction.amount.toFixed(2)}`
                      : `$${transaction.amount.toFixed(2)}`}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleDelete(transaction.id)}
                      className="text-red-600 hover:text-red-900"
                      title="Delete"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {transactions.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No transactions found.</p>
          </div>
        )}
      </div>
    </div>
  );
};
