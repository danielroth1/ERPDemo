#!/bin/bash

# Deploy to Remote Server Script
# Deploys the ERP system to shopping-now.net server

set -e

# ── Load local deploy config ─────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Missing $ENV_FILE"
    echo "   Copy the example and fill in your values:"
    echo "   cp .env.deploy.example .env.deploy"
    exit 1
fi
# shellcheck source=.env.deploy
source "$ENV_FILE"

SERVER="${DEPLOY_SERVER}"
REMOTE_DIR="${DEPLOY_REMOTE_DIR}"
LOCAL_DIR="${DEPLOY_LOCAL_DIR}"

# Parse services to deploy
SERVICES=""
while [[ $# -gt 0 ]]; do
    case $1 in
        --services)
            shift
            SERVICES="$1"
            shift
            ;;
        *)
            COMMAND="$1"
            shift
            ;;
    esac
done

echo "========================================="
echo "Deploy ERP System to Server"
echo "Server: $SERVER"
if [ -n "$SERVICES" ]; then
    echo "Services: $SERVICES"
else
    echo "Services: ALL"
fi
echo "========================================="
echo ""

# Function to check SSH connection
check_ssh() {
    echo "🔍 Checking SSH connection..."
    if ssh -o ConnectTimeout=5 "$SERVER" "echo 'Connected'" &>/dev/null; then
        echo "✅ SSH connection successful"
        return 0
    else
        echo "❌ Cannot connect to server via SSH"
        echo "Please check:"
        echo "  1. Server is running"
        echo "  2. SSH key is configured"
        echo "  3. Network connection is available"
        exit 1
    fi
}

# Function to sync files
sync_files() {
    echo ""
    echo "📦 Syncing files to server..."
    echo "This may take a few minutes..."
    
    # Create remote directory if it doesn't exist
    ssh "$SERVER" "mkdir -p $REMOTE_DIR"
    
    # Sync files excluding unnecessary items
    rsync -avz --progress \
        --exclude 'node_modules' \
        --exclude '.git' \
        --exclude '.vscode' \
        --exclude 'bin' \
        --exclude 'obj' \
        --exclude '**/bin' \
        --exclude '**/obj' \
        --exclude 'ssl-certs' \
        --exclude '.env' \
        --exclude 'docker-compose.override.yml' \
        "$LOCAL_DIR/" "$SERVER:$REMOTE_DIR/"
    
    echo "✅ Files synced successfully"
}

# Function to setup environment
setup_environment() {
    echo ""
    echo "⚙️  Setting up environment on server..."
    
    ssh "$SERVER" bash -s -- "$REMOTE_DIR" << 'ENDSSH'
REMOTE_DIR="$1"
cd "$REMOTE_DIR"

# Stop any existing containers
echo "Stopping existing containers..."
docker-compose down 2>/dev/null || true

# Create .env file if it doesn't exist
if [ ! -f .env ]; then
    echo "Creating .env file from example..."
    cp .env.example .env
fi

echo "✅ Environment setup complete"
ENDSSH
}

# Function to build and start containers
start_containers() {
    echo ""
    echo "🐳 Building and starting containers on server..."
    
    if [ -n "$SERVICES" ]; then
        ssh "$SERVER" bash << ENDSSH
cd "$REMOTE_DIR"

# Build and start specific services
docker-compose up -d --build $SERVICES

# Wait a moment for containers to initialize
sleep 5

# Show status
echo ""
echo "Container status:"
docker-compose ps $SERVICES

echo ""
echo "✅ Services started: $SERVICES"
ENDSSH
    else
        ssh "$SERVER" bash -s -- "$REMOTE_DIR" << 'ENDSSH'
REMOTE_DIR="$1"
cd "$REMOTE_DIR"

# Build and start all containers
docker-compose up -d --build

# Wait a moment for containers to initialize
sleep 5

# Show status
echo ""
echo "Container status:"
docker-compose ps

echo ""
echo "✅ All containers started"
ENDSSH
    fi
}

# Function to check deployment
check_deployment() {
    echo ""
    echo "🔍 Checking deployment..."
    
    ssh "$SERVER" bash -s -- "$REMOTE_DIR" << 'ENDSSH'
REMOTE_DIR="$1"
cd "$REMOTE_DIR"

# Check which containers are running
RUNNING=$(docker-compose ps --filter "status=running" --services | wc -l)
TOTAL=$(docker-compose ps --services | wc -l)

echo "Running containers: $RUNNING / $TOTAL"

# Check if any containers failed
FAILED=$(docker-compose ps --filter "status=exited" --services)
if [ -n "$FAILED" ]; then
    echo ""
    echo "⚠️  Failed containers:"
    echo "$FAILED"
    echo ""
    echo "Checking logs..."
    for service in $FAILED; do
        echo "--- Logs for $service ---"
        docker-compose logs --tail=20 "$service"
        echo ""
    done
fi
ENDSSH
}

# Function to show next steps
show_next_steps() {
    echo ""
    echo "========================================="
    echo "✅ Deployment Complete!"
    echo "========================================="
    echo ""
    echo "Services are running on internal ports:"
    echo "  Frontend: http://$(ssh "$SERVER" 'hostname -I | awk "{print \$1}"'):8081"
    echo "  Gateway: http://$(ssh "$SERVER" 'hostname -I | awk "{print \$1}"'):8080"
    echo "  Kafka UI: http://$(ssh "$SERVER" 'hostname -I | awk "{print \$1}"'):9001"
    echo ""
    echo "⚠️  To make shopping-now.net accessible via HTTPS:"
    echo "  1. Configure nginx reverse proxy (see below)"
    echo "  2. Setup SSL certificates"
    echo ""
    echo "Next steps:"
    echo "1. Configure nginx reverse proxy:"
    echo "   ssh $SERVER"
    echo "   sudo nano /etc/nginx/sites-available/shopping-now.net"
    echo ""
    echo "2. Check logs:"
    echo "   ./deploy.sh logs"
    echo "   ./deploy.sh logs frontend"
    echo ""
    echo "3. Deploy specific services only:"
    echo "   ./deploy.sh deploy --services \"frontend gateway\""
    echo ""
    echo "4. Manage containers:"
    echo "   ssh $SERVER"
    echo "   cd $REMOTE_DIR"
    echo "   ./manage-containers.sh"
    echo ""
}

# Main execution
main() {
    check_ssh
    sync_files
    setup_environment
    start_containers
    check_deployment
    show_next_steps
}

# Parse command (default: deploy)
COMMAND="${COMMAND:-deploy}"

case "$COMMAND" in
    deploy)
        main
        ;;
    sync)
        check_ssh
        sync_files
        ;;
    start)
        check_ssh
        start_containers
        ;;
    check)
        check_ssh
        check_deployment
        ;;
    logs)
        SERVICE="${SERVICES:-}"
        if [ -n "$SERVICE" ]; then
            ssh "$SERVER" "cd $REMOTE_DIR && docker-compose logs -f $SERVICE"
        else
            ssh "$SERVER" "cd $REMOTE_DIR && docker-compose logs -f"
        fi
        ;;
    ssh)
        ssh "$SERVER" "cd $REMOTE_DIR && bash"
        ;;
    *)
        echo "Usage: $0 [COMMAND] [--services \"service1 service2 ...\"]"
        echo ""
        echo "Commands:"
        echo "  deploy  - Full deployment (sync + start + check)"
        echo "  sync    - Sync files to server only"
        echo "  start   - Start containers on server"
        echo "  check   - Check deployment status"
        echo "  logs    - View logs"
        echo "  ssh     - SSH to server and navigate to project directory"
        echo ""
        echo "Options:"
        echo "  --services \"service1 service2\"  Deploy/manage specific services only"
        echo ""
        echo "Examples:"
        echo "  $0 deploy                              # Deploy all services"
        echo "  $0 deploy --services \"frontend\"        # Deploy only frontend"
        echo "  $0 deploy --services \"gateway frontend\" # Deploy gateway and frontend"
        echo "  $0 logs --services \"frontend\"          # View frontend logs"
        echo "  $0 start --services \"kafka mongodb\"    # Start only kafka and mongodb"
        exit 1
        ;;
esac
