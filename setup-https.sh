#!/bin/bash

# HTTPS Setup Script for shopping-now.net
# This script sets up SSL certificates using Let's Encrypt (Certbot)

set -e

DOMAIN="shopping-now.net"
EMAIL="admin@shopping-now.net"  # Change this to your email
CERT_DIR="./ssl-certs"

echo "========================================="
echo "HTTPS Setup for $DOMAIN"
echo "========================================="

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "⚠️  This script should be run as root or with sudo"
    echo "Run: sudo ./setup-https.sh"
    exit 1
fi

# Install certbot if not already installed
if ! command -v certbot &> /dev/null; then
    echo "📦 Installing certbot..."
    apt-get update
    apt-get install -y certbot
fi

# Create certificate directory
mkdir -p "$CERT_DIR"

echo ""
echo "========================================="
echo "Step 1: Obtain SSL Certificate"
echo "========================================="
echo ""
echo "Choose an option:"
echo "1) Standalone mode (requires port 80 to be free)"
echo "2) Webroot mode (use if nginx is already running)"
echo "3) Skip (certificates already exist)"
read -p "Enter choice [1-3]: " choice

case $choice in
    1)
        echo "🔒 Obtaining certificate using standalone mode..."
        certbot certonly --standalone \
            -d $DOMAIN \
            -d www.$DOMAIN \
            --non-interactive \
            --agree-tos \
            --email $EMAIL
        ;;
    2)
        echo "🔒 Obtaining certificate using webroot mode..."
        certbot certonly --webroot \
            -w /usr/share/nginx/html \
            -d $DOMAIN \
            -d www.$DOMAIN \
            --non-interactive \
            --agree-tos \
            --email $EMAIL
        ;;
    3)
        echo "⏭️  Skipping certificate generation..."
        ;;
    *)
        echo "❌ Invalid choice"
        exit 1
        ;;
esac

# Copy certificates to project directory
if [ "$choice" != "3" ]; then
    echo ""
    echo "📋 Copying certificates to project directory..."
    cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem "$CERT_DIR/"
    cp /etc/letsencrypt/live/$DOMAIN/privkey.pem "$CERT_DIR/"
    chmod 644 "$CERT_DIR/fullchain.pem"
    chmod 600 "$CERT_DIR/privkey.pem"
    echo "✅ Certificates copied to $CERT_DIR/"
fi

echo ""
echo "========================================="
echo "Step 2: Update docker-compose.yml"
echo "========================================="
echo ""
echo "Add the following volume mount to the frontend service:"
echo ""
echo "    volumes:"
echo "      - ./ssl-certs:/etc/nginx/ssl:ro"
echo ""
read -p "Press Enter to continue..."

echo ""
echo "========================================="
echo "Step 3: Restart Docker Containers"
echo "========================================="
echo ""
read -p "Restart containers now? [y/N]: " restart

if [[ $restart =~ ^[Yy]$ ]]; then
    echo "🔄 Restarting containers..."
    docker-compose down
    docker-compose up -d --build frontend
    echo "✅ Containers restarted"
fi

echo ""
echo "========================================="
echo "✅ HTTPS Setup Complete!"
echo "========================================="
echo ""
echo "Your site should now be accessible at:"
echo "  https://$DOMAIN"
echo "  https://www.$DOMAIN"
echo ""
echo "Certificate renewal:"
echo "  Certbot will auto-renew certificates."
echo "  Test renewal with: sudo certbot renew --dry-run"
echo ""
echo "To manually renew and restart:"
echo "  sudo certbot renew"
echo "  cp /etc/letsencrypt/live/$DOMAIN/*.pem $CERT_DIR/"
echo "  docker-compose restart frontend"
echo ""
