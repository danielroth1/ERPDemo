import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { inventoryService } from '../../services/inventory.service';
import type { Product, Category } from '../../types';

interface InventoryState {
  products: Product[];
  categories: Category[];
  selectedProduct: Product | null;
  isLoading: boolean;
  error: string | null;
  totalProducts: number;
  currentPage: number;
}

const initialState: InventoryState = {
  products: [],
  categories: [],
  selectedProduct: null,
  isLoading: false,
  error: null,
  totalProducts: 0,
  currentPage: 1,
};

export const fetchProducts = createAsyncThunk(
  'inventory/fetchProducts',
  async ({ page, pageSize }: { page: number; pageSize: number }, { rejectWithValue }) => {
    try {
      return await inventoryService.getProducts(page, pageSize);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch products');
    }
  }
);

export const fetchCategories = createAsyncThunk(
  'inventory/fetchCategories',
  async (_, { rejectWithValue }) => {
    try {
      return await inventoryService.getCategories();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch categories');
    }
  }
);

export const createProduct = createAsyncThunk(
  'inventory/createProduct',
  async (product: Omit<Product, 'id' | 'createdAt' | 'updatedAt'>, { rejectWithValue }) => {
    try {
      return await inventoryService.createProduct(product);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create product');
    }
  }
);

export const updateProduct = createAsyncThunk(
  'inventory/updateProduct',
  async ({ id, product }: { id: string; product: Partial<Product> }, { rejectWithValue }) => {
    try {
      return await inventoryService.updateProduct(id, product);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update product');
    }
  }
);

export const deleteProduct = createAsyncThunk(
  'inventory/deleteProduct',
  async (id: string, { rejectWithValue }) => {
    try {
      await inventoryService.deleteProduct(id);
      return id;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete product');
    }
  }
);

export const createCategory = createAsyncThunk(
  'inventory/createCategory',
  async (category: Omit<Category, 'id' | 'createdAt' | 'updatedAt'>, { rejectWithValue }) => {
    try {
      return await inventoryService.createCategory(category);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create category');
    }
  }
);

export const seedProducts = createAsyncThunk(
  'inventory/seedProducts',
  async (_, { rejectWithValue }) => {
    try {
      return await inventoryService.seedProducts();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to seed products');
    }
  }
);

const inventorySlice = createSlice({
  name: 'inventory',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setSelectedProduct: (state, action) => {
      state.selectedProduct = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch products
      .addCase(fetchProducts.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchProducts.fulfilled, (state, action) => {
        state.isLoading = false;
        state.products = action.payload.items;
        state.totalProducts = action.payload.totalCount;
        state.currentPage = action.payload.page;
      })
      .addCase(fetchProducts.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      })
      // Fetch categories
      .addCase(fetchCategories.fulfilled, (state, action) => {
        state.categories = action.payload;
      })
      // Create product
      .addCase(createProduct.fulfilled, (state, action) => {
        state.products.unshift(action.payload);
        state.totalProducts += 1;
      })
      // Update product
      .addCase(updateProduct.fulfilled, (state, action) => {
        const index = state.products.findIndex((p) => p.id === action.payload.id);
        if (index !== -1) {
          state.products[index] = action.payload;
        }
      })
      // Delete product
      .addCase(deleteProduct.fulfilled, (state, action) => {
        state.products = state.products.filter((p) => p.id !== action.payload);
        state.totalProducts -= 1;
      })
      // Create category
      .addCase(createCategory.fulfilled, (state, action) => {
        state.categories.push(action.payload);
      })
      // Seed products
      .addCase(seedProducts.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(seedProducts.fulfilled, (state) => {
        state.isLoading = false;
      })
      .addCase(seedProducts.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });
  },
});

export const { clearError, setSelectedProduct } = inventorySlice.actions;
export default inventorySlice.reducer;
