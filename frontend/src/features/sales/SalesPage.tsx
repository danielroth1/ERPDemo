import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchOrders, fetchCustomers, updateOrderStatus, deleteOrder } from './salesSlice';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ShoppingCartIcon, TrashIcon } from '@heroicons/react/24/outline';
import { OrderStatus } from '../../types';
import toast from 'react-hot-toast';

export const SalesPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { orders, isLoading, totalOrders } = useAppSelector((state) => state.sales);
  const pageSize = 10;

  useEffect(() => {
    dispatch(fetchOrders({ page: 1, pageSize }));
    dispatch(fetchCustomers());
  }, [dispatch]);

  const handleStatusChange = async (id: string, status: string) => {
    try {
      await dispatch(updateOrderStatus({ id, status })).unwrap();
      toast.success('Order status updated successfully');
    } catch (error: any) {
      toast.error(error || 'Failed to update order status');
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this order?')) {
      try {
        await dispatch(deleteOrder(id)).unwrap();
        toast.success('Order deleted successfully');
      } catch (error: any) {
        toast.error(error || 'Failed to delete order');
      }
    }
  };

  const getStatusBadgeColor = (status: string) => {
    switch (status) {
      case OrderStatus.Pending:
        return 'bg-yellow-100 text-yellow-800';
      case OrderStatus.Confirmed:
        return 'bg-blue-100 text-blue-800';
      case OrderStatus.Shipped:
        return 'bg-purple-100 text-purple-800';
      case OrderStatus.Delivered:
        return 'bg-green-100 text-green-800';
      case OrderStatus.Cancelled:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading && orders.length === 0) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Sales & Orders</h1>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <ShoppingCartIcon className="h-10 w-10 text-blue-500" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Total Orders</p>
              <p className="text-2xl font-bold text-gray-900">{totalOrders}</p>
            </div>
          </div>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Pending</p>
          <p className="text-2xl font-bold text-yellow-600">
            {orders.filter((o) => o.status === OrderStatus.Pending).length}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Shipped</p>
          <p className="text-2xl font-bold text-purple-600">
            {orders.filter((o) => o.status === OrderStatus.Shipped).length}
          </p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Delivered</p>
          <p className="text-2xl font-bold text-green-600">
            {orders.filter((o) => o.status === OrderStatus.Delivered).length}
          </p>
        </div>
      </div>

      {/* Orders Table */}
      <div className="card">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Order #
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Customer
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Items
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Total
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Date
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {orders.map((order) => (
                <tr key={order.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    #{order.orderNumber}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {order.customer?.name || order.customerId}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {order.items.length} items
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-900">
                    ${order.totalAmount.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <select
                      value={order.status}
                      onChange={(e) => handleStatusChange(order.id, e.target.value)}
                      className={`px-2 py-1 text-xs leading-5 font-semibold rounded-full ${getStatusBadgeColor(
                        order.status
                      )} border-0 cursor-pointer`}
                    >
                      <option value={OrderStatus.Pending}>Pending</option>
                      <option value={OrderStatus.Confirmed}>Confirmed</option>
                      <option value={OrderStatus.Shipped}>Shipped</option>
                      <option value={OrderStatus.Delivered}>Delivered</option>
                      <option value={OrderStatus.Cancelled}>Cancelled</option>
                    </select>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(order.orderDate).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleDelete(order.id)}
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

        {orders.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No orders found.</p>
          </div>
        )}
      </div>
    </div>
  );
};
