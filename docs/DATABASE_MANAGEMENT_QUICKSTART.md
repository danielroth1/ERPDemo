# Database Management Quick Start

## Prerequisites

- All services running (use `dev-setup` task)
- Redis running (`docker-compose up -d redis`)
- Admin or Manager role access

## Access the Database Management Page

1. Start all services:
```powershell
# Terminal 1: Start infrastructure
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Terminal 2: Start backend services (in VS Code)
# Tasks -> Run Task -> dev-setup

# Terminal 3: Start frontend
cd frontend
npm run dev
```

2. Navigate to: **http://localhost:5173/database**

## Quick Tour

### 1. Overview Tab
- View all 5 MongoDB databases at a glance
- See total statistics across all services
- Expand service cards to see collections
- Click collections to view indexes and schema

### 2. Search Tab
- Enter search terms to find collections
- Filter by service, collection name, or size
- Set document count ranges
- View matched fields in results

### 3. Query Tab (Admin Only)
- Select service and collection
- Choose query type (Find/Count/Aggregate)
- Write MongoDB query in JSON
- Execute and view results
- See execution time and result count

### 4. Alerts Tab
- View database health alerts
- See high document counts
- Monitor large collections
- Track resolved alerts

## Common Tasks

### View All Collections

1. Go to Overview tab
2. Click any service card to expand
3. Click a collection to see details

### Search for a Collection

1. Go to Search tab
2. Enter collection name
3. Click "Search"

### Execute a Simple Query

1. Go to Query tab (Admin only)
2. Select service: "Inventory"
3. Select collection: "products"
4. Choose query type: "Find"
5. Enter query: `{ "isActive": true }`
6. Set limit: 10
7. Click "Execute Query"

### Monitor Real-Time Updates

1. Enable "Auto-refresh" toggle in header
2. Watch for live update indicator (green dot)
3. Updates appear as toast notifications

### Clear Cache

1. Click "Clear Cache" button in header
2. Confirm action
3. Fresh data loads automatically

## Tips

- **Performance**: Use cache for frequent queries
- **Security**: Query execution is logged and audited
- **Limits**: Results limited to 100 by default (max 1000)
- **Safety**: Dangerous keywords ($where, eval) are blocked

## Troubleshooting

### "Failed to load database overview"
- Check MongoDB connection
- Verify services are running
- Check browser console for errors

### "Unauthorized" errors
- Verify you're logged in
- Check your role (Admin/Manager required)
- Refresh JWT token

### WebSocket not connecting
- Check GraphQL endpoint: `ws://localhost:5005/graphql`
- Verify JWT token in localStorage
- Check browser WebSocket support

### Query execution fails
- Validate JSON syntax
- Check for dangerous keywords
- Verify collection exists
- Review query execution history

## Keyboard Shortcuts

- `Enter` in search field: Execute search
- `Ctrl+Enter` in query editor: Execute query (planned)

## Next Steps

- Review [Full Documentation](./DATABASE_MANAGEMENT.md)
- Explore GraphQL API
- Set up custom alerts
- Create query templates

## Support

For issues or questions, check:
1. Browser console logs
2. Backend service logs
3. MongoDB connection status
4. Redis connection status
