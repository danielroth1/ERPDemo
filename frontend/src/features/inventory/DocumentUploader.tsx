import React, { useRef, useState } from 'react';
import {
  DocumentArrowDownIcon,
  TrashIcon,
  XMarkIcon,
  ArrowUpTrayIcon,
} from '@heroicons/react/24/outline';
import { inventoryService } from '../../services/inventory.service';
import type { ProductDocument } from '../../types';
import toast from 'react-hot-toast';

interface Props {
  productId: string;
  documents: ProductDocument[];
  isAdmin: boolean;
  onDocumentsChange: (docs: ProductDocument[]) => void;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export const DocumentUploader: React.FC<Props> = ({
  productId,
  documents,
  isAdmin,
  onDocumentsChange,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const [uploadFileName, setUploadFileName] = useState('');
  const abortRef = useRef<AbortController | null>(null);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = '';

    const controller = new AbortController();
    abortRef.current = controller;
    setUploadFileName(file.name);
    setProgress(0);

    try {
      const doc = await inventoryService.uploadDocument(
        productId,
        file,
        (pct) => setProgress(pct),
        controller.signal,
      );
      onDocumentsChange([doc, ...documents]);
      toast.success(`"${file.name}" uploaded`);
    } catch (err) {
      if ((err as Error)?.name === 'AbortError') {
        toast('Upload cancelled');
      } else {
        toast.error((err as Error)?.message ?? 'Document upload failed');
      }
    } finally {
      setProgress(null);
      setUploadFileName('');
      abortRef.current = null;
    }
  };

  const handleDelete = async (doc: ProductDocument) => {
    if (!confirm(`Delete "${doc.originalFileName}"?`)) return;
    try {
      await inventoryService.deleteDocument(productId, doc.id);
      onDocumentsChange(documents.filter((d) => d.id !== doc.id));
      toast.success('Document deleted');
    } catch {
      toast.error('Failed to delete document');
    }
  };

  const handleDownload = (doc: ProductDocument) => {
    // The backend returns a 302 to a presigned MinIO URL. Open in new tab so
    // the browser handles the redirect and triggers a file download.
    const url = inventoryService.getDocumentDownloadUrl(productId, doc.id);
    const a = document.createElement('a');
    a.href = url;
    a.target = '_blank';
    a.rel = 'noopener noreferrer';
    a.click();
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
          Documents
        </h3>
        {isAdmin && (
          <button
            onClick={() => inputRef.current?.click()}
            disabled={progress !== null}
            className="btn btn-secondary btn-sm flex items-center gap-1.5"
          >
            <ArrowUpTrayIcon className="h-4 w-4" />
            Upload
          </button>
        )}
      </div>

      {/* Upload progress */}
      {progress !== null && (
        <div className="mb-3 p-3 border border-primary-200 bg-primary-50 rounded-lg">
          <div className="flex items-center justify-between mb-1.5">
            <span className="text-xs text-gray-700 truncate max-w-[70%]">{uploadFileName}</span>
            <span className="text-xs font-medium text-primary-700">{progress}%</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-1.5">
            <div
              className="bg-primary-500 h-1.5 rounded-full transition-all duration-150"
              style={{ width: `${progress}%` }}
            />
          </div>
          <button
            onClick={() => abortRef.current?.abort()}
            className="mt-2 flex items-center gap-1 text-xs text-red-600 hover:text-red-800"
          >
            <XMarkIcon className="h-3 w-3" /> Cancel upload
          </button>
        </div>
      )}

      {/* Document list */}
      {documents.length === 0 && progress === null ? (
        <p className="text-sm text-gray-400 italic">No documents attached.</p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {documents.map((doc) => (
            <li
              key={doc.id}
              className="flex items-center gap-3 py-2 group"
            >
              <DocumentArrowDownIcon className="h-5 w-5 text-gray-400 flex-shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 truncate">
                  {doc.originalFileName}
                </p>
                <p className="text-xs text-gray-500">
                  {formatBytes(doc.sizeBytes)} &middot;{' '}
                  {new Date(doc.uploadedAt).toLocaleDateString()}
                </p>
              </div>
              <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  onClick={() => handleDownload(doc)}
                  title="Download"
                  className="p-1 text-primary-600 hover:text-primary-800 rounded"
                >
                  <DocumentArrowDownIcon className="h-4 w-4" />
                </button>
                {isAdmin && (
                  <button
                    onClick={() => handleDelete(doc)}
                    title="Delete"
                    className="p-1 text-red-500 hover:text-red-700 rounded"
                  >
                    <TrashIcon className="h-4 w-4" />
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      <input
        ref={inputRef}
        type="file"
        className="hidden"
        onChange={handleFileChange}
      />
    </div>
  );
};
