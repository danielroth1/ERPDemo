#!/bin/bash

# Removes deployed MongoDB on the
# Run this when you change MONGODB_ROOT_PASSWORD in .env or manually change the password

SERVER="${DEPLOY_SERVER}"

echo "========================================="
echo "Fix MongoDB Authentication"
echo "========================================="
echo ""
echo "⚠️  WARNING: This will:"
echo "  1. Stop all containers"
echo "  2. DELETE MongoDB data volume"
echo "  3. Restart with new password"
echo ""
echo "All data in MongoDB will be LOST!"
echo ""
read -p "Are you sure? Type 'yes' to continue: " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 1
fi

ssh "$SERVER" bash << 'ENDSSH'
cd /home/daniel/ERPDemo

echo ""
echo "📊 Current container status:"
docker-compose ps

echo ""
echo "🛑 Stopping all containers..."
docker-compose down

echo ""
echo "🗑️  Removing MongoDB volume..."
docker volume ls | grep mongodb
docker volume rm erpdemo_mongodb-data 2>/dev/null || echo "Volume not found (already removed)"

echo ""
echo "🚀 Starting containers with fresh MongoDB..."
docker-compose up -d

echo ""
echo "⏳ Waiting 15 seconds for MongoDB to initialize..."
sleep 15

echo ""
echo "📊 New container status:"
docker-compose ps

echo ""
echo "🔍 Checking MongoDB health..."
docker-compose logs mongodb | tail -20

echo ""
echo "🔍 Checking user-management service logs..."
docker-compose logs user-management | tail -30

ENDSSH

echo ""
echo "========================================="
echo "✅ MongoDB Reset Complete!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Check if containers are running: ./deploy.sh status"
echo "2. View logs: ./deploy.sh logs"
echo "3. Test health: curl http://shopping-now.net/health"
echo ""
