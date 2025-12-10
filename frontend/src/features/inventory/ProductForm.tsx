import React, { useState } from 'react';
import { useAppDispatch } from '../../store/hooks';
import { createProduct, updateProduct, createCategory } from './inventorySlice';
import type { Product, Category } from '../../types';
import toast from 'react-hot-toast';

interface ProductFormProps {
  product?: Product | null;
  categories: Category[];
  onClose: () => void;
}

export const ProductForm: React.FC<ProductFormProps> = ({ product, categories, onClose }) => {
  const dispatch = useAppDispatch();
  const [formData, setFormData] = useState({
    name: product?.name || '',
    description: product?.description || '',
    sku: product?.sku || '',
    categoryId: product?.categoryId || '',
    unitPrice: product?.unitPrice || 0,
    stockQuantity: product?.stockQuantity || 0,
    reorderLevel: product?.reorderLevel || 10,
    isActive: product?.isActive ?? true,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isNewCategory, setIsNewCategory] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target;
    
    // Handle category selection change
    if (name === 'categoryId' && value === '__new__') {
      setIsNewCategory(true);
      setFormData({ ...formData, categoryId: '' });
      return;
    } else if (name === 'categoryId' && isNewCategory && value !== '__new__') {
      setIsNewCategory(false);
      setNewCategoryName('');
    }
    
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? (e.target as HTMLInputElement).checked : 
              type === 'number' ? parseFloat(value) || 0 : value,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      let categoryId = formData.categoryId;
      
      // Create new category if needed
      if (isNewCategory && newCategoryName.trim()) {
        const newCategory = await dispatch(createCategory({ 
          name: newCategoryName.trim(),
          description: '' 
        })).unwrap();
        categoryId = newCategory.id;
        toast.success('Category created successfully');
      }
      
      // Create or update product
      const productData = { ...formData, categoryId };
      
      if (product) {
        await dispatch(updateProduct({ id: product.id, product: productData })).unwrap();
        toast.success('Product updated successfully');
      } else {
        await dispatch(createProduct(productData)).unwrap();
        toast.success('Product created successfully');
      }
      onClose();
    } catch (error: any) {
      toast.error(error || 'Operation failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor="name" className="label">
            Product Name *
          </label>
          <input
            type="text"
            id="name"
            name="name"
            required
            className="input"
            value={formData.name}
            onChange={handleChange}
          />
        </div>

        <div>
          <label htmlFor="sku" className="label">
            SKU *
          </label>
          <input
            type="text"
            id="sku"
            name="sku"
            required
            className="input"
            value={formData.sku}
            onChange={handleChange}
          />
        </div>
      </div>

      <div>
        <label htmlFor="description" className="label">
          Description
        </label>
        <textarea
          id="description"
          name="description"
          rows={3}
          className="input"
          value={formData.description}
          onChange={handleChange}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor="categoryId" className="label">
            Category *
          </label>
          {!isNewCategory ? (
            <select
              id="categoryId"
              name="categoryId"
              required
              className="input"
              value={formData.categoryId}
              onChange={handleChange}
            >
              <option value="">Select a category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
              <option value="__new__">+ Create New Category</option>
            </select>
          ) : (
            <div className="space-y-2">
              <input
                type="text"
                id="newCategoryName"
                name="newCategoryName"
                required
                placeholder="Enter new category name"
                className="input"
                value={newCategoryName}
                onChange={(e) => setNewCategoryName(e.target.value)}
              />
              <button
                type="button"
                onClick={() => {
                  setIsNewCategory(false);
                  setNewCategoryName('');
                }}
                className="text-sm text-gray-600 hover:text-gray-800 underline"
              >
                ← Back to category list
              </button>
            </div>
          )}
        </div>

        <div>
          <label htmlFor="unitPrice" className="label">
            Unit Price *
          </label>
          <input
            type="number"
            id="unitPrice"
            name="unitPrice"
            required
            step="0.01"
            min="0"
            className="input"
            value={formData.unitPrice}
            onChange={handleChange}
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor="stockQuantity" className="label">
            Stock Quantity *
          </label>
          <input
            type="number"
            id="stockQuantity"
            name="stockQuantity"
            required
            min="0"
            className="input"
            value={formData.stockQuantity}
            onChange={handleChange}
          />
        </div>

        <div>
          <label htmlFor="reorderLevel" className="label">
            Reorder Level *
          </label>
          <input
            type="number"
            id="reorderLevel"
            name="reorderLevel"
            required
            min="0"
            className="input"
            value={formData.reorderLevel}
            onChange={handleChange}
          />
        </div>
      </div>

      <div className="flex items-center">
        <input
          type="checkbox"
          id="isActive"
          name="isActive"
          className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
          checked={formData.isActive}
          onChange={handleChange}
        />
        <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
          Active
        </label>
      </div>

      <div className="flex justify-end gap-3 pt-4">
        <button type="button" onClick={onClose} className="btn btn-secondary">
          Cancel
        </button>
        <button type="submit" disabled={isSubmitting} className="btn btn-primary">
          {isSubmitting ? 'Saving...' : product ? 'Update' : 'Create'}
        </button>
      </div>
    </form>
  );
};
