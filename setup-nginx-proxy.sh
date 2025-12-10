#!/bin/bash

# Setup nginx reverse proxy for shopping-now.net
# This script creates the config file and shows you manual steps
# It does NOT automatically modify your existing nginx setup

set -e

DOMAIN="shopping-now.net"
EMAIL="admin@shopping-now.net"  # Change this to your email
CONFIG_DIR="/home/daniel/ERPDemo/infrastructure/nginx"
NGINX_AVAILABLE="/etc/nginx/sites-available"
NGINX_ENABLED="/etc/nginx/sites-enabled"

echo "========================================="
echo "nginx Configuration Helper for $DOMAIN"
echo "========================================="
echo ""

# Check if nginx config exists
if [ -f "$NGINX_AVAILABLE/shopping-now.net" ]; then
    echo "⚠️  Configuration already exists at:"
    echo "   $NGINX_AVAILABLE/shopping-now.net"
    echo ""
    read -p "Overwrite? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Aborted."
        exit 0
    fi
fi

# Copy the configuration file
echo "📝 Copying nginx configuration..."
if [ -f "$CONFIG_DIR/shopping-now.net.conf" ]; then
    sudo cp "$CONFIG_DIR/shopping-now.net.conf" "$NGINX_AVAILABLE/shopping-now.net"
    echo "✅ Configuration file created at: $NGINX_AVAILABLE/shopping-now.net"
else
    echo "❌ Source config not found: $CONFIG_DIR/shopping-now.net.conf"
    exit 1
fi

echo ""
echo "========================================="
echo "📋 Manual Steps Required"
echo "========================================="
echo ""
echo "The configuration file has been created but NOT activated."
echo "This preserves your existing nginx setup."
echo ""
echo "To complete the setup, run these commands manually:"
echo ""
echo "1️⃣  Get SSL certificate (if you don't have one yet):"
echo "   sudo certbot certonly --nginx -d $DOMAIN -d www.$DOMAIN --email $EMAIL"
echo ""
echo "2️⃣  Enable the configuration:"
echo "   sudo ln -s $NGINX_AVAILABLE/shopping-now.net $NGINX_ENABLED/shopping-now.net"
echo ""
echo "3️⃣  Test nginx configuration:"
echo "   sudo nginx -t"
echo ""
echo "4️⃣  If test passes, reload nginx:"
echo "   sudo systemctl reload nginx"
echo ""
echo "5️⃣  (Optional) Setup SSL auto-renewal if not already configured:"
echo "   sudo certbot renew --dry-run"
echo ""
echo "========================================="
echo "ℹ️  Additional Information"
echo "========================================="
echo ""
echo "View existing nginx sites:"
echo "  ls -la $NGINX_ENABLED"
echo ""
echo "Edit the configuration if needed:"
echo "  sudo nano $NGINX_AVAILABLE/shopping-now.net"
echo ""
echo "Check nginx error logs if issues occur:"
echo "  sudo tail -f /var/log/nginx/error.log"
echo ""
echo "The configuration routes:"
echo "  https://$DOMAIN/     → http://localhost:8081 (Frontend)"
echo "  https://$DOMAIN/api/ → http://localhost:8080 (API Gateway)"
echo ""
