import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchProducts, fetchCategories, deleteProduct, seedProducts } from './inventorySlice';
import { Modal } from '../../components/common/Modal';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ProductForm } from './ProductForm';
import { PlusIcon, PencilIcon, TrashIcon, SparklesIcon, ChevronLeftIcon, ChevronRightIcon, TagIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import { inventoryService } from '../../services/inventory.service';

export const InventoryPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { products, categories, isLoading, totalProducts } = useAppSelector(
    (state) => state.inventory
  );
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<any>(null);
  const [isSeeding, setIsSeeding] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [showCategoryManager, setShowCategoryManager] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<{ id: string; name: string; productCount: number } | null>(null);
  const [categoryCounts, setCategoryCounts] = useState<Record<string, number>>({});
  const pageSize = 10;

  useEffect(() => {
    dispatch(fetchProducts({ page: currentPage, pageSize }));
    dispatch(fetchCategories());
  }, [dispatch, currentPage]);

  // Load category product counts when category manager opens
  useEffect(() => {
    if (showCategoryManager && categories.length > 0) {
      loadCategoryCounts();
    }
  }, [showCategoryManager, categories]);

  const loadCategoryCounts = async () => {
    const counts: Record<string, number> = {};
    await Promise.all(
      categories.map(async (category) => {
        try {
          const count = await inventoryService.getCategoryProductCount(category.id);
          counts[category.id] = count;
        } catch (error) {
          counts[category.id] = 0;
        }
      })
    );
    setCategoryCounts(counts);
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this product?')) {
      try {
        await dispatch(deleteProduct(id)).unwrap();
        toast.success('Product deleted successfully');
      } catch (error: any) {
        toast.error(error || 'Failed to delete product');
      }
    }
  };

  const handleSeedProducts = async () => {
    if (confirm('This will delete all existing products and create sample products. Continue?')) {
      setIsSeeding(true);
      try {
        const result = await dispatch(seedProducts()).unwrap();
        toast.success(`Created ${result.productsCreated} products (deleted ${result.productsDeleted} existing)`);
        // Refresh products list
        setCurrentPage(1);
        await dispatch(fetchProducts({ page: 1, pageSize }));
        await dispatch(fetchCategories());
      } catch (error: any) {
        toast.error(error || 'Failed to seed products');
      } finally {
        setIsSeeding(false);
      }
    }
  };

  const handleDeleteCategory = async (categoryId: string, categoryName: string) => {
    // Fetch actual product count from server
    try {
      const productCount = await inventoryService.getCategoryProductCount(categoryId);
      
      if (productCount > 0) {
        setCategoryToDelete({ id: categoryId, name: categoryName, productCount });
      } else {
        // No products, delete immediately
        if (confirm(`Delete category "${categoryName}"?`)) {
          try {
            await inventoryService.deleteCategory(categoryId);
            toast.success('Category deleted successfully');
            await dispatch(fetchCategories());
          } catch (error: any) {
            toast.error(error?.message || 'Failed to delete category');
          }
        }
      }
    } catch (error: any) {
      toast.error(error?.message || 'Failed to check category products');
    }
  };

  const confirmDeleteCategory = async () => {
    if (!categoryToDelete) return;
    
    try {
      await inventoryService.deleteCategory(categoryToDelete.id);
      toast.success('Category and associated products deleted successfully');
      setCategoryToDelete(null);
      await dispatch(fetchCategories());
      await dispatch(fetchProducts({ page: currentPage, pageSize }));
    } catch (error: any) {
      toast.error(error?.message || 'Failed to delete category');
    }
  };

  const handleEdit = (product: any) => {
    setEditingProduct(product);
    setIsModalOpen(true);
  };

  const handleCreate = () => {
    setEditingProduct(null);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingProduct(null);
  };

  const getCategoryName = (categoryId: string) => {
    return categories.find((c) => c.id === categoryId)?.name || 'Unknown';
  };

  const totalPages = Math.ceil(totalProducts / pageSize);

  const handleNextPage = () => {
    if (currentPage < totalPages) {
      setCurrentPage(currentPage + 1);
    }
  };

  const handlePrevPage = () => {
    if (currentPage > 1) {
      setCurrentPage(currentPage - 1);
    }
  };

  const handlePageClick = (page: number) => {
    setCurrentPage(page);
  };

  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisible = 5;
    
    if (totalPages <= maxVisible) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (currentPage <= 3) {
        for (let i = 1; i <= 4; i++) pages.push(i);
        pages.push('...');
        pages.push(totalPages);
      } else if (currentPage >= totalPages - 2) {
        pages.push(1);
        pages.push('...');
        for (let i = totalPages - 3; i <= totalPages; i++) pages.push(i);
      } else {
        pages.push(1);
        pages.push('...');
        for (let i = currentPage - 1; i <= currentPage + 1; i++) pages.push(i);
        pages.push('...');
        pages.push(totalPages);
      }
    }
    
    return pages;
  };

  if (isLoading && products.length === 0) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Inventory Management</h1>
        <div className="flex gap-3">
          <button 
            onClick={() => setShowCategoryManager(!showCategoryManager)}
            className="btn btn-secondary flex items-center gap-2"
          >
            <TagIcon className="h-5 w-5" />
            Manage Categories
          </button>
          <button 
            onClick={handleSeedProducts} 
            disabled={isSeeding}
            className="btn btn-secondary flex items-center gap-2"
          >
            <SparklesIcon className="h-5 w-5" />
            {isSeeding ? 'Seeding...' : 'Seed Sample Products'}
          </button>
          <button onClick={handleCreate} className="btn btn-primary flex items-center gap-2">
            <PlusIcon className="h-5 w-5" />
            Add Product
          </button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <div className="card">
          <p className="text-sm text-gray-600">Total Products</p>
          <p className="text-3xl font-bold text-gray-900">{totalProducts}</p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Categories</p>
          <p className="text-3xl font-bold text-gray-900">{categories.length}</p>
        </div>
        <div className="card">
          <p className="text-sm text-gray-600">Low Stock Items</p>
          <p className="text-3xl font-bold text-red-600">
            {products.filter((p) => p.stockQuantity <= p.reorderLevel).length}
          </p>
        </div>
      </div>

      {/* Products Table */}
      <div className="card">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Product
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  SKU
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Category
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Price
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Stock
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {products.map((product) => (
                <tr key={product.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{product.name}</div>
                    <div className="text-sm text-gray-500">{product.description}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {product.sku}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {getCategoryName(product.categoryId)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    ${product.unitPrice.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`text-sm font-medium ${
                        product.stockQuantity <= product.reorderLevel
                          ? 'text-red-600'
                          : 'text-gray-900'
                      }`}
                    >
                      {product.stockQuantity}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        product.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-gray-100 text-gray-800'
                      }`}
                    >
                      {product.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleEdit(product)}
                      className="text-primary-600 hover:text-primary-900 mr-4"
                    >
                      <PencilIcon className="h-5 w-5" />
                    </button>
                    <button
                      onClick={() => handleDelete(product.id)}
                      className="text-red-600 hover:text-red-900"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {products.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No products found. Create your first product!</p>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="px-6 py-4 flex items-center justify-between border-t border-gray-200">
            <div className="flex-1 flex justify-between sm:hidden">
              <button
                onClick={handlePrevPage}
                disabled={currentPage === 1}
                className="btn btn-secondary btn-sm"
              >
                Previous
              </button>
              <button
                onClick={handleNextPage}
                disabled={currentPage === totalPages}
                className="btn btn-secondary btn-sm"
              >
                Next
              </button>
            </div>
            <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
              <div>
                <p className="text-sm text-gray-700">
                  Showing <span className="font-medium">{(currentPage - 1) * pageSize + 1}</span> to{' '}
                  <span className="font-medium">
                    {Math.min(currentPage * pageSize, totalProducts)}
                  </span>{' '}
                  of <span className="font-medium">{totalProducts}</span> results
                </p>
              </div>
              <div>
                <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px">
                  <button
                    onClick={handlePrevPage}
                    disabled={currentPage === 1}
                    className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronLeftIcon className="h-5 w-5" />
                  </button>
                  {getPageNumbers().map((page, index) =>
                    typeof page === 'number' ? (
                      <button
                        key={index}
                        onClick={() => handlePageClick(page)}
                        className={`relative inline-flex items-center px-4 py-2 border text-sm font-medium ${
                          currentPage === page
                            ? 'z-10 bg-primary-50 border-primary-500 text-primary-600'
                            : 'bg-white border-gray-300 text-gray-500 hover:bg-gray-50'
                        }`}
                      >
                        {page}
                      </button>
                    ) : (
                      <span
                        key={index}
                        className="relative inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-sm font-medium text-gray-700"
                      >
                        {page}
                      </span>
                    )
                  )}
                  <button
                    onClick={handleNextPage}
                    disabled={currentPage === totalPages}
                    className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronRightIcon className="h-5 w-5" />
                  </button>
                </nav>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Category Manager Modal */}
      <Modal
        isOpen={showCategoryManager}
        onClose={() => setShowCategoryManager(false)}
        title="Manage Categories"
        size="md"
      >
        <div className="space-y-4">
          {categories.length === 0 ? (
            <p className="text-gray-500 text-center py-8">No categories found</p>
          ) : (
            <div className="space-y-2">
              {categories.map((category) => {
                const productCount = categoryCounts[category.id] ?? 0;
                return (
                  <div
                    key={category.id}
                    className="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
                  >
                    <div>
                      <h3 className="font-medium text-gray-900">{category.name}</h3>
                      <p className="text-sm text-gray-500">{category.description}</p>
                      <p className="text-xs text-gray-400 mt-1">
                        {productCount} product{productCount !== 1 ? 's' : ''}
                      </p>
                    </div>
                    <button
                      onClick={() => handleDeleteCategory(category.id, category.name)}
                      className="text-red-600 hover:text-red-900"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </Modal>

      {/* Delete Category Confirmation Modal */}
      <Modal
        isOpen={!!categoryToDelete}
        onClose={() => setCategoryToDelete(null)}
        title="Confirm Category Deletion"
        size="sm"
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3">
            <ExclamationTriangleIcon className="h-6 w-6 text-yellow-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-gray-700">
                Deleting category <strong>"{categoryToDelete?.name}"</strong> will also delete{' '}
                <strong>{categoryToDelete?.productCount}</strong> associated product
                {categoryToDelete?.productCount !== 1 ? 's' : ''}.
              </p>
              <p className="text-sm text-gray-600 mt-2">This action cannot be undone.</p>
            </div>
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <button
              onClick={() => setCategoryToDelete(null)}
              className="btn btn-secondary"
            >
              Cancel
            </button>
            <button
              onClick={confirmDeleteCategory}
              className="btn btn-danger"
            >
              Delete Category & Products
            </button>
          </div>
        </div>
      </Modal>

      {/* Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        title={editingProduct ? 'Edit Product' : 'Add Product'}
        size="lg"
      >
        <ProductForm
          product={editingProduct}
          categories={categories}
          onClose={handleCloseModal}
        />
      </Modal>
    </div>
  );
};
