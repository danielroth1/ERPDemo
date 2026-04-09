import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeftIcon,
  PencilIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  TagIcon,
  CubeIcon,
  BanknotesIcon,
} from '@heroicons/react/24/outline';
import { useAppSelector } from '../../store/hooks';
import { inventoryService } from '../../services/inventory.service';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { Modal } from '../../components/common/Modal';
import { ProductForm } from './ProductForm';
import { ImageUploader } from './ImageUploader';
import { DocumentUploader } from './DocumentUploader';
import type { Product, ProductDocument, Category } from '../../types';
import toast from 'react-hot-toast';

export const ItemDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAppSelector((state) => state.auth);
  const isAdmin = user?.roles?.some(role => role === 2) ?? false; // Assuming role '2' is Admin

  const [product, setProduct] = useState<Product | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [categoryName, setCategoryName] = useState('');

  useEffect(() => {
    if (!id) return;
    loadProduct(id);
  }, [id]);

  const loadProduct = async (productId: string) => {
    setIsLoading(true);
    try {
      const [p, cats] = await Promise.all([
        inventoryService.getProduct(productId),
        inventoryService.getCategories(),
      ]);
      setProduct(p);
      setCategories(cats);

      if (p.categoryId) {
        const cat = cats.find((c) => c.id === p.categoryId);
        setCategoryName(cat?.name ?? '');
      }
    } catch {
      toast.error('Failed to load product');
      navigate(-1);
    } finally {
      setIsLoading(false);
    }
  };

  const handleImageChange = (url: string | null) => {
    setProduct((prev) => (prev ? { ...prev, imageUrl: url } : prev));
  };

  const handleDocumentsChange = (docs: ProductDocument[]) => {
    setProduct((prev) => (prev ? { ...prev, documents: docs } : prev));
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (!product) return null;

  const isLowStock = product.stockQuantity <= product.reorderLevel;
  const stockColor = isLowStock ? 'text-red-600' : 'text-green-600';

  return (
    <div className="max-w-6xl mx-auto">
      {/* Breadcrumb / back nav */}
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-800 mb-6 transition-colors"
      >
        <ArrowLeftIcon className="h-4 w-4" />
        Back to Inventory
      </button>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Left column — image */}
        <div>
          <ImageUploader
            productId={product.id}
            currentImageUrl={product.imageUrl}
            isAdmin={isAdmin}
            onImageChange={handleImageChange}
          />
        </div>

        {/* Right column — product info */}
        <div className="flex flex-col gap-6">
          {/* Header */}
          <div>
            <div className="flex items-start justify-between gap-4">
              <h1 className="text-2xl font-bold text-gray-900 leading-tight">
                {product.name}
              </h1>
              {isAdmin && (
                <button
                  onClick={() => setIsEditOpen(true)}
                  className="btn btn-secondary flex-shrink-0 flex items-center gap-1.5"
                >
                  <PencilIcon className="h-4 w-4" />
                  Edit
                </button>
              )}
            </div>

            <p className="mt-1 text-lg font-semibold text-gray-500">
              SKU: {product.sku}
            </p>
          </div>

          {/* Price */}
          <div>
            <p className="text-3xl font-bold text-gray-900">
              ${product.unitPrice.toFixed(2)}
            </p>
          </div>

          {/* Status badges */}
          <div className="flex flex-wrap gap-2">
            <span
              className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold ${
                product.isActive
                  ? 'bg-green-100 text-green-800'
                  : 'bg-gray-100 text-gray-600'
              }`}
            >
              <CheckCircleIcon className="h-3.5 w-3.5" />
              {product.isActive ? 'Active' : 'Inactive'}
            </span>

            {isLowStock && (
              <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold bg-red-100 text-red-700">
                <ExclamationTriangleIcon className="h-3.5 w-3.5" />
                Low Stock
              </span>
            )}

            {categoryName && (
              <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700">
                <TagIcon className="h-3.5 w-3.5" />
                {categoryName}
              </span>
            )}
          </div>

          {/* Description */}
          {product.description && (
            <div>
              <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-1">
                Description
              </h2>
              <p className="text-gray-600 leading-relaxed whitespace-pre-line">
                {product.description}
              </p>
            </div>
          )}

          {/* Details grid */}
          <div className="grid grid-cols-2 gap-4">
            <div className="card !p-4">
              <div className="flex items-center gap-2 text-gray-500 mb-1">
                <CubeIcon className="h-4 w-4" />
                <span className="text-xs font-medium uppercase tracking-wide">Stock</span>
              </div>
              <p className={`text-2xl font-bold ${stockColor}`}>
                {product.stockQuantity}
              </p>
              <p className="text-xs text-gray-400">{product.unit ?? 'pcs'}</p>
            </div>

            <div className="card !p-4">
              <div className="flex items-center gap-2 text-gray-500 mb-1">
                <ExclamationTriangleIcon className="h-4 w-4" />
                <span className="text-xs font-medium uppercase tracking-wide">Reorder at</span>
              </div>
              <p className="text-2xl font-bold text-gray-800">{product.reorderLevel}</p>
              <p className="text-xs text-gray-400">{product.unit ?? 'pcs'}</p>
            </div>

            {isAdmin && (
              <div className="card !p-4">
                <div className="flex items-center gap-2 text-gray-500 mb-1">
                  <BanknotesIcon className="h-4 w-4" />
                  <span className="text-xs font-medium uppercase tracking-wide">Cost price</span>
                </div>
                <p className="text-2xl font-bold text-gray-800">
                  ${(product as unknown as { cost?: number }).cost?.toFixed(2) ?? '—'}
                </p>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Documents section — full width below the two columns */}
      <div className="mt-10 card">
        <DocumentUploader
          productId={product.id}
          documents={product.documents ?? []}
          isAdmin={isAdmin}
          onDocumentsChange={handleDocumentsChange}
        />
      </div>

      {/* Edit product modal */}
      <Modal
        isOpen={isEditOpen}
        onClose={() => setIsEditOpen(false)}
        title="Edit Product"
        size="lg"
      >
        <ProductForm
          product={product}
          categories={categories}
          onClose={() => { setIsEditOpen(false); if (id) loadProduct(id); }}
        />
      </Modal>
    </div>
  );
};
