import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchUsers, deleteUser, toggleUserStatus } from './usersSlice';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { UserGroupIcon, CheckCircleIcon, XCircleIcon, TrashIcon, WalletIcon, PlusIcon } from '@heroicons/react/24/outline';
import { UserRole, type Account } from '../../types';
import toast from 'react-hot-toast';
import { financialService } from '../../services/financial.service';

export const UsersPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { users, isLoading, totalUsers } = useAppSelector((state) => state.users);
  const currentUser = useAppSelector((state) => state.auth.user);
  const [userAccounts, setUserAccounts] = useState<Record<string, Account[]>>({});
  const [loadingAccounts, setLoadingAccounts] = useState<Record<string, boolean>>({});
  const pageSize = 10;

  useEffect(() => {
    dispatch(fetchUsers({ page: 1, pageSize }));
  }, [dispatch]);

  const loadUserAccounts = async (userId: string) => {
    //if (userAccounts[userId]) return; // Already loaded
    
    setLoadingAccounts(prev => ({ ...prev, [userId]: true }));
    try {
      const accounts = await financialService.getUserAccounts(userId);
      setUserAccounts(prev => ({ ...prev, [userId]: accounts }));
    } catch (error: any) {
      toast.error('Failed to load user accounts');
    } finally {
      setLoadingAccounts(prev => ({ ...prev, [userId]: false }));
    }
  };

  const handleCreateAccount = async (userId: string, userName: string) => {
    try {
      await financialService.createUserAccount(userId, userName);
      toast.success('Account created successfully');
      // Reload accounts
      setUserAccounts(prev => {
        const newAccounts = { ...prev };
        delete newAccounts[userId];
        return newAccounts;
      });
      await loadUserAccounts(userId);
    } catch (error: any) {
      toast.error(error?.message || 'Failed to create account');
    }
  };

  const handleDeleteAccount = async (userId: string, accountId: string) => {
    if (!confirm('Are you sure you want to deactivate this account?')) return;
    
    try {
      await financialService.deleteUserAccount(accountId);
      toast.success('Account deactivated successfully');
      // Reload accounts
      setUserAccounts(prev => {
        const newAccounts = { ...prev };
        delete newAccounts[userId];
        return newAccounts;
      });
      await loadUserAccounts(userId);
    } catch (error: any) {
      toast.error(error?.message || 'Failed to deactivate account');
    }
  };

  const handleToggleStatus = async (id: string, isActive: boolean) => {
    try {
      await dispatch(toggleUserStatus({ id, isActive })).unwrap();
      toast.success(`User ${isActive ? 'deactivated' : 'activated'} successfully`);
    } catch (error: any) {
      toast.error(error || 'Failed to update user status');
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this user?')) {
      try {
        await dispatch(deleteUser(id)).unwrap();
        toast.success('User deleted successfully');
      } catch (error: any) {
        toast.error(error || 'Failed to delete user');
      }
    }
  };

  const getRoleBadgeColor = (role: string) => {
    switch (role) {
      case UserRole.Admin:
        return 'bg-purple-100 text-purple-800';
      case UserRole.Manager:
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading && users.length === 0) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <UserGroupIcon className="h-10 w-10 text-blue-500" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Total Users</p>
              <p className="text-2xl font-bold text-gray-900">{totalUsers}</p>
            </div>
          </div>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Active Users</p>
          <p className="text-2xl font-bold text-green-600">
            {users.filter((u) => u.isActive).length}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Admins</p>
          <p className="text-2xl font-bold text-purple-600">
            {users.filter((u) => u.roles?.includes(2)).length}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Managers</p>
          <p className="text-2xl font-bold text-blue-600">
            {users.filter((u) => u.roles?.includes(1)).length}
          </p>
        </div>
      </div>

      {/* Users Table */}
      <div className="card">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  User
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Email
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Role
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Accounts
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Joined
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {users.map((user) => (
                <tr key={user.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{user.firstName} {user.lastName}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {user.email}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getRoleBadgeColor(
                        user.roles?.[0] === 2 ? UserRole.Admin : user.roles?.[0] === 1 ? UserRole.Manager : UserRole.User
                      )}`}
                    >
                      {user.roles?.[0] === 2 ? 'Admin' : user.roles?.[0] === 1 ? 'Manager' : 'User'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        user.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-red-100 text-red-800'
                      }`}
                    >
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => loadUserAccounts(user.id)}
                        className="text-blue-600 hover:text-blue-900"
                        title="View Accounts"
                      >
                        <WalletIcon className="h-5 w-5" />
                      </button>
                      <button
                        onClick={() => handleCreateAccount(user.id, `${user.firstName} ${user.lastName}`)}
                        className="text-green-600 hover:text-green-900"
                        title="Create Account"
                      >
                        <PlusIcon className="h-5 w-5" />
                      </button>
                      {userAccounts[user.id] && (
                        <div className="text-xs text-gray-600">
                          {loadingAccounts[user.id] ? (
                            <span>Loading...</span>
                          ) : (
                            <span>{userAccounts[user.id].length} account(s)</span>
                          )}
                        </div>
                      )}
                    </div>
                    {userAccounts[user.id] && userAccounts[user.id].length > 0 && (
                      <div className="mt-2 space-y-1">
                        {userAccounts[user.id].map(account => (
                          <div key={account.id} className="flex items-center justify-between text-xs bg-gray-50 px-2 py-1 rounded">
                            <span className="text-gray-700">
                              {account.name} - ${account.balance.toFixed(2)}
                            </span>
                            <button
                              onClick={() => handleDeleteAccount(user.id, account.id)}
                              className="text-red-600 hover:text-red-900"
                              title="Deactivate Account"
                            >
                              <TrashIcon className="h-4 w-4" />
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    {currentUser?.id !== user.id && (
                      <>
                        <button
                          onClick={() => handleToggleStatus(user.id, user.isActive)}
                          className={`${
                            user.isActive ? 'text-orange-600 hover:text-orange-900' : 'text-green-600 hover:text-green-900'
                          } mr-4`}
                          title={user.isActive ? 'Deactivate' : 'Activate'}
                        >
                          {user.isActive ? (
                            <XCircleIcon className="h-5 w-5" />
                          ) : (
                            <CheckCircleIcon className="h-5 w-5" />
                          )}
                        </button>
                        <button
                          onClick={() => handleDelete(user.id)}
                          className="text-red-600 hover:text-red-900"
                          title="Delete"
                        >
                          <TrashIcon className="h-5 w-5" />
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {users.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No users found.</p>
          </div>
        )}
      </div>
    </div>
  );
};
