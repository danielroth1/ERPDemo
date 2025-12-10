import React, { useEffect, useState } from 'react';
import {
  ShoppingCartIcon,
  CubeIcon,
  ArrowPathIcon,
  WalletIcon,
  PlusIcon,
  MinusIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import { shopService, type ShopProduct, type ShopCategory } from '../../services/shop.service';
import { financialService } from '../../services/financial.service';
import { useAppSelector } from '../../store/hooks';

export const ShopPage: React.FC = () => {
  const [products, setProducts] = useState<ShopProduct[]>([]);
  const [categories, setCategories] = useState<ShopCategory[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [balance, setBalance] = useState<number>(0);
  const [currency, setCurrency] = useState<string>('USD');
  const [accountId, setAccountId] = useState<string>('');
  const user = useAppSelector((state) => state.auth.user);

  useEffect(() => {
    loadData();
  }, [selectedCategory]);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Get user-specific account
      let accountData = null;
      if (user?.id) {
        const userAccounts = await financialService.getUserAccounts(user.id);
        accountData = userAccounts[0] || null;
      }

      const [productsData, categoriesData] = await Promise.all([
        shopService.getAvailableProducts(selectedCategory || undefined),
        shopService.getCategories(),
      ]);

      setProducts(productsData);
      setCategories(categoriesData);
      
      if (accountData) {
        setBalance(accountData.balance);
        setCurrency('USD'); // accountData.currency might not exist
        setAccountId(accountData.id);
      }
    } catch (error: any) {
      toast.error(error?.message || 'Failed to load shop data');
    } finally {
      setLoading(false);
    }
  };

  const handlePurchase = async (productId: string, productName: string, price: number) => {
    if (balance < price) {
      toast.error('Insufficient balance to purchase this item');
      return;
    }

    try {
      await shopService.purchaseProduct(productId);
      toast.success(`Successfully purchased ${productName}`);
      await loadData(); // Refresh products and balance
    } catch (error: any) {
      toast.error(error?.message || 'Failed to purchase product');
    }
  };

  const handleReturn = async (productId: string, productName: string) => {
    try {
      await shopService.returnProduct(productId);
      toast.success(`Successfully returned ${productName}`);
      await loadData(); // Refresh products and balance
    } catch (error: any) {
      toast.error(error?.message || 'Failed to return product');
    }
  };

  const handleIncreaseBalance = async () => {
    if (!user || !user.roles?.some(role => role === 2)) {
      toast.error('Only admins can adjust account balance');
      return;
    }

    try {
      await financialService.adjustAccountBalance(accountId, 100);
      toast.success('Balance increased by $100');
      await loadData();
    } catch (error: any) {
      toast.error(error?.message || 'Failed to increase balance');
    }
  };

  const handleDecreaseBalance = async () => {
    if (!user || !user.roles?.some(role => role === 2)) {
      toast.error('Only admins can adjust account balance');
      return;
    }

    try {
      await financialService.adjustAccountBalance(accountId, -100);
      toast.success('Balance decreased by $100');
      await loadData();
    } catch (error: any) {
      toast.error(error?.message || 'Failed to decrease balance');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
            <ShoppingCartIcon className="w-8 h-8" />
            Shop
          </h1>
          <p className="text-gray-600 mt-1">Browse and purchase products</p>
        </div>
        
        {/* Balance Display */}
        <div className="bg-white rounded-lg shadow-md p-4 border-2 border-primary-200">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <WalletIcon className="w-6 h-6 text-primary-600" />
              <div>
                <p className="text-sm text-gray-600">Current Balance</p>
                <p className="text-2xl font-bold text-gray-900">
                  {new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: currency
                  }).format(balance)}
                </p>
              </div>
            </div>
            
            {user?.roles?.some(role => role === 2) && (
              <div className="flex gap-2 ml-4 border-l pl-4">
                <button
                  onClick={handleIncreaseBalance}
                  className="btn btn-sm btn-success flex items-center gap-1"
                  title="Increase balance by $100"
                >
                  <PlusIcon className="w-4 h-4" />
                  Add $100
                </button>
                <button
                  onClick={handleDecreaseBalance}
                  className="btn btn-sm btn-secondary flex items-center gap-1"
                  title="Decrease balance by $100"
                >
                  <MinusIcon className="w-4 h-4" />
                  Remove $100
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Category Filter */}
      <div className="bg-white rounded-lg shadow p-4">
        <h2 className="text-lg font-semibold mb-3">Filter by Category</h2>
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setSelectedCategory(null)}
            className={`px-4 py-2 rounded-lg transition-colors ${
              selectedCategory === null
                ? 'bg-primary-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            All Products
          </button>
          {categories.map((category) => (
            <button
              key={category.id}
              onClick={() => setSelectedCategory(category.id ?? null)}
              className={`px-4 py-2 rounded-lg transition-colors ${
                selectedCategory === category.id
                  ? 'bg-primary-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {category.name}
            </button>
          ))}
        </div>
      </div>

      {/* Products Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {products.length === 0 ? (
          <div className="col-span-full text-center py-12 text-gray-500">
            <CubeIcon className="w-16 h-16 mx-auto mb-4 opacity-50" />
            <p>No products available</p>
          </div>
        ) : (
          products.map((product) => (
            <div
              key={product.id}
              className="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow"
            >
              <div className="p-6">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <h3 className="text-lg font-semibold text-gray-900 mb-1">
                      {product.name}
                    </h3>
                    <p className="text-sm text-gray-600 mb-2 line-clamp-2">
                      {product.description || 'No description available'}
                    </p>
                    <p className="text-xs text-gray-500">SKU: {product.sku}</p>
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-gray-600">Price:</span>
                    <span className="text-xl font-bold text-primary-600">
                      {new Intl.NumberFormat('en-US', {
                        style: 'currency',
                        currency: currency
                      }).format(product.price ?? 0)}
                    </span>
                  </div>

                  <div className="flex justify-between items-center text-sm">
                    <span className="text-gray-600">In Stock:</span>
                    <span className={`font-semibold ${
                      (product.stockQuantity ?? 0) > 10 ? 'text-green-600' : 'text-orange-600'
                    }`}>
                      {product.stockQuantity ?? 0} {product.unit}
                    </span>
                  </div>

                  <div className="pt-3 border-t flex gap-2">
                    <button
                      onClick={() => handlePurchase(product.id ?? '', product.name ?? '', product.price ?? 0)}
                      disabled={product.stockQuantity === 0 || balance < (product.price ?? 0)}
                      className="btn btn-primary btn-sm flex-1 flex items-center justify-center gap-2"
                      title={balance < (product.price ?? 0) ? 'Insufficient balance' : 'Purchase this item'}
                    >
                      <ShoppingCartIcon className="w-4 h-4" />
                      Purchase
                    </button>
                    <button
                      onClick={() => handleReturn(product.id ?? '', product.name ?? '')}
                      className="btn btn-secondary btn-sm flex-1 flex items-center justify-center gap-2"
                      title="Return this item"
                    >
                      <ArrowPathIcon className="w-4 h-4" />
                      Return
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
