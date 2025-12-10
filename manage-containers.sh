#!/bin/bash

# Docker Container Management Script
# Helps with cleaning up and replacing outdated containers

set -e

echo "========================================="
echo "Docker Container Management"
echo "========================================="
echo ""

# Function to show menu
show_menu() {
    echo "Choose an option:"
    echo "1) Stop all containers"
    echo "2) Remove all containers"
    echo "3) Remove all containers and images"
    echo "4) Full cleanup (containers, images, volumes, networks)"
    echo "5) Rebuild and restart all containers"
    echo "6) Rebuild and restart specific service"
    echo "7) View container status"
    echo "8) View container logs"
    echo "9) Exit"
    echo ""
}

# Stop all containers
stop_containers() {
    echo "🛑 Stopping all containers..."
    docker-compose down
    echo "✅ All containers stopped"
}

# Remove all containers
remove_containers() {
    echo "🗑️  Removing all containers..."
    docker-compose down -v
    echo "✅ All containers removed"
}

# Remove containers and images
remove_containers_and_images() {
    echo "🗑️  Removing all containers and images..."
    docker-compose down --rmi all -v
    echo "✅ All containers and images removed"
}

# Full cleanup
full_cleanup() {
    echo "⚠️  WARNING: This will remove ALL containers, images, volumes, and networks!"
    read -p "Are you sure? [y/N]: " confirm
    if [[ $confirm =~ ^[Yy]$ ]]; then
        echo "🗑️  Performing full cleanup..."
        docker-compose down --rmi all -v --remove-orphans
        docker system prune -af --volumes
        echo "✅ Full cleanup complete"
    else
        echo "❌ Cleanup cancelled"
    fi
}

# Rebuild and restart all
rebuild_all() {
    echo "🔄 Rebuilding and restarting all containers..."
    docker-compose down
    docker-compose up -d --build
    echo "✅ All containers rebuilt and started"
}

# Rebuild specific service
rebuild_service() {
    echo ""
    echo "Available services:"
    echo "  - gateway"
    echo "  - user-management"
    echo "  - inventory"
    echo "  - sales"
    echo "  - financial"
    echo "  - dashboard"
    echo "  - frontend"
    echo ""
    read -p "Enter service name: " service
    
    if [ -z "$service" ]; then
        echo "❌ No service specified"
        return
    fi
    
    echo "🔄 Rebuilding $service..."
    docker-compose up -d --build --force-recreate $service
    echo "✅ $service rebuilt and restarted"
}

# View status
view_status() {
    echo "📊 Container Status:"
    echo ""
    docker-compose ps
    echo ""
    echo "Press Enter to continue..."
    read
}

# View logs
view_logs() {
    echo ""
    read -p "Enter service name (or 'all' for all services): " service
    
    if [ "$service" = "all" ]; then
        docker-compose logs --tail=50
    else
        docker-compose logs --tail=50 $service
    fi
    
    echo ""
    read -p "Follow logs? [y/N]: " follow
    if [[ $follow =~ ^[Yy]$ ]]; then
        if [ "$service" = "all" ]; then
            docker-compose logs -f
        else
            docker-compose logs -f $service
        fi
    fi
}

# Main loop
while true; do
    show_menu
    read -p "Enter choice [1-9]: " choice
    echo ""
    
    case $choice in
        1) stop_containers ;;
        2) remove_containers ;;
        3) remove_containers_and_images ;;
        4) full_cleanup ;;
        5) rebuild_all ;;
        6) rebuild_service ;;
        7) view_status ;;
        8) view_logs ;;
        9) 
            echo "👋 Goodbye!"
            exit 0
            ;;
        *)
            echo "❌ Invalid choice"
            ;;
    esac
    
    echo ""
    echo "Press Enter to continue..."
    read
    clear
done
