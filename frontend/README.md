# ERP System - React Frontend

Modern React frontend with real-time updates for the ERP system.

## Tech Stack

- React 19 + TypeScript + Vite 7
- Redux Toolkit for state management
- TailwindCSS 4 for styling
- Apollo Client for GraphQL
- SignalR for real-time updates
- React Router 7 for routing

## Quick Start

```bash
npm install
npm run dev
```

Visit http://localhost:5173

## Features Implemented

### Core Infrastructure ✅
- Authentication (Login/Register with JWT)
- Protected routes with automatic redirects
- REST API integration with auto token refresh
- GraphQL setup (Apollo Client)
- SignalR WebSocket for real-time updates
- Redux Toolkit state management
- Responsive layout with collapsible sidebar
- TypeScript type safety throughout
- Toast notifications

### Feature Modules ✅
- **Dashboard**: Real-time metrics with SignalR subscriptions
- **Inventory**: Full CRUD for products, categories, stock movements
- **Users**: User management with role-based access (Admin/Manager/User)
- **Sales**: Order management with status workflow, customer tracking
- **Financial**: Transaction management, account balances, reports
- **Analytics**: KPIs, alerts, top products, real-time dashboard

## Build & Deploy

```bash
# Production build (576KB bundle, 175KB gzipped)
npm run build

# Docker
docker build -t erp-frontend .
docker run -p 80:80 erp-frontend
```

## Module Status

All feature modules **complete** ✅
- **Dashboard**: ✅ Real-time metrics, SignalR integration
- **Inventory**: ✅ Products, categories, stock management
- **Users**: ✅ User CRUD, role management, activate/deactivate
- **Sales**: ✅ Orders, customers, status workflow
- **Financial**: ✅ Transactions, accounts, balance calculations
- **Analytics**: ✅ KPIs, alerts, top products, summary stats
- **Financial**: 🚧 Placeholder
- **Analytics**: 🚧 Placeholder

Build succeeded: 532KB bundle (168KB gzipped)
