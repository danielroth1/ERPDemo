import React, { useRef, useState } from 'react';
import { PhotoIcon, XMarkIcon, ArrowUpTrayIcon } from '@heroicons/react/24/outline';
import { inventoryService } from '../../services/inventory.service';
import toast from 'react-hot-toast';

interface Props {
  productId: string;
  currentImageUrl?: string | null;
  isAdmin: boolean;
  onImageChange: (url: string | null) => void;
}

export const ImageUploader: React.FC<Props> = ({
  productId,
  currentImageUrl,
  isAdmin,
  onImageChange,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const handleClick = () => {
    if (isAdmin) inputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = '';

    const controller = new AbortController();
    abortRef.current = controller;
    setProgress(0);

    try {
      const url = await inventoryService.uploadImage(
        productId,
        file,
        (pct) => setProgress(pct),
        controller.signal,
      );
      onImageChange(url);
      toast.success('Image uploaded');
    } catch (err) {
      if ((err as Error)?.name === 'AbortError') {
        toast('Upload cancelled');
      } else {
        toast.error((err as Error)?.message ?? 'Image upload failed');
      }
    } finally {
      setProgress(null);
      abortRef.current = null;
    }
  };

  const handleCancel = () => {
    abortRef.current?.abort();
  };

  const handleRemove = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await inventoryService.deleteImage(productId);
      onImageChange(null);
      toast.success('Image removed');
    } catch {
      toast.error('Failed to remove image');
    }
  };

  return (
    <div className="relative">
      {/* Image / placeholder */}
      <div
        onClick={handleClick}
        className={`aspect-square w-full rounded-xl overflow-hidden bg-gray-100 border-2 border-dashed border-gray-300 flex items-center justify-center ${
          isAdmin && progress === null ? 'cursor-pointer hover:border-primary-400 hover:bg-gray-50 transition-colors' : ''
        }`}
      >
        {currentImageUrl ? (
          <img
            src={currentImageUrl}
            alt="Product"
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="flex flex-col items-center gap-2 text-gray-400 select-none">
            <PhotoIcon className="h-16 w-16" />
            {isAdmin && (
              <span className="text-sm font-medium">Click to upload image</span>
            )}
          </div>
        )}
      </div>

      {/* Upload progress overlay */}
      {progress !== null && (
        <div className="absolute inset-0 bg-black/50 rounded-xl flex flex-col items-center justify-center gap-3 p-6">
          <span className="text-white text-sm font-medium">{progress}%</span>
          <div className="w-full bg-white/30 rounded-full h-2">
            <div
              className="bg-white h-2 rounded-full transition-all duration-150"
              style={{ width: `${progress}%` }}
            />
          </div>
          <button
            onClick={handleCancel}
            className="flex items-center gap-1 text-white text-xs border border-white/50 rounded px-3 py-1 hover:bg-white/20"
          >
            <XMarkIcon className="h-3 w-3" /> Cancel
          </button>
        </div>
      )}

      {/* Admin controls */}
      {isAdmin && progress === null && (
        <div className="absolute top-2 right-2 flex gap-1">
          <button
            onClick={handleClick}
            title="Upload image"
            className="p-1.5 bg-white/90 rounded-lg shadow text-gray-700 hover:bg-white"
          >
            <ArrowUpTrayIcon className="h-4 w-4" />
          </button>
          {currentImageUrl && (
            <button
              onClick={handleRemove}
              title="Remove image"
              className="p-1.5 bg-white/90 rounded-lg shadow text-red-600 hover:bg-white"
            >
              <XMarkIcon className="h-4 w-4" />
            </button>
          )}
        </div>
      )}

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp,image/gif"
        className="hidden"
        onChange={handleFileChange}
      />
    </div>
  );
};
