-- ============================================
-- PostgreSQL Database Setup Script
-- ============================================

-- Create the database (run this while connected to 'postgres' database)
-- Note: You cannot be connected to a database while dropping/creating it
-- DROP DATABASE IF EXISTS erp_demo;
CREATE DATABASE erp_demo
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_United States.1252'
    LC_CTYPE = 'English_United States.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- After creating database, connect to it with: \c erp_demo
-- Then run the rest of this script

-- ============================================
-- Create Schema
-- ============================================
CREATE SCHEMA IF NOT EXISTS app_data;
SET search_path TO app_data, public;

-- ============================================
-- Create Custom Types
-- ============================================

-- User status enum
CREATE TYPE app_data.user_status AS ENUM ('active', 'inactive', 'suspended');

-- Item category enum
CREATE TYPE app_data.item_category AS ENUM ('electronics', 'clothing', 'food', 'books', 'other');

-- Email domain with validation
CREATE DOMAIN app_data.email_address AS VARCHAR(255)
    CHECK (VALUE ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');

-- ============================================
-- Create Users Table
-- ============================================
CREATE TABLE app_data.users (
    -- Primary key with auto-increment
    user_id SERIAL PRIMARY KEY,
    
    -- UUID for external references
    user_uuid UUID DEFAULT gen_random_uuid() UNIQUE NOT NULL,
    
    -- User credentials
    email app_data.email_address UNIQUE NOT NULL,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    
    -- User details
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    status app_data.user_status DEFAULT 'active',
    
    -- JSON preferences (PostgreSQL JSONB is binary and indexable)
    preferences JSONB DEFAULT '{}',
    
    -- Array of roles (PostgreSQL native array type)
    roles TEXT[] DEFAULT ARRAY['user'],
    
    -- Timestamps with timezone
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE,
    
    -- Constraints
    CONSTRAINT username_length CHECK (length(username) >= 3),
    CONSTRAINT valid_roles CHECK (roles <@ ARRAY['user', 'admin', 'manager', 'viewer'])
);

-- Create indexes on users
CREATE INDEX idx_users_email ON app_data.users(email);
CREATE INDEX idx_users_username ON app_data.users(username);
CREATE INDEX idx_users_status ON app_data.users(status) WHERE status = 'active';

-- GIN index for JSONB (great for JSON queries)
CREATE INDEX idx_users_preferences ON app_data.users USING GIN(preferences);

-- GIN index for array queries
CREATE INDEX idx_users_roles ON app_data.users USING GIN(roles);

-- Full-text search index
CREATE INDEX idx_users_search ON app_data.users 
    USING GIN(to_tsvector('english', coalesce(username, '') || ' ' || coalesce(first_name, '') || ' ' || coalesce(last_name, '')));

-- ============================================
-- Create Items Table
-- ============================================
CREATE TABLE app_data.items (
    -- Primary key
    item_id SERIAL PRIMARY KEY,
    
    -- UUID for external references
    item_uuid UUID DEFAULT gen_random_uuid() UNIQUE NOT NULL,
    
    -- Foreign key to users
    owner_id INTEGER REFERENCES app_data.users(user_id) ON DELETE CASCADE,
    
    -- Item details
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category app_data.item_category NOT NULL,
    sku VARCHAR(50) UNIQUE,
    
    -- Price with precision (NUMERIC is better than FLOAT for money)
    price NUMERIC(10, 2) CHECK (price >= 0),
    cost NUMERIC(10, 2) CHECK (cost >= 0),
    quantity INTEGER DEFAULT 0 CHECK (quantity >= 0),
    
    -- Array of tags
    tags TEXT[] DEFAULT '{}',
    
    -- JSONB for flexible attributes
    attributes JSONB DEFAULT '{}',
    
    -- Range type for availability period
    availability_period TSTZRANGE,
    
    -- Point type for warehouse location (x, y coordinates)
    warehouse_location POINT,
    
    -- Generated column for full-text search
    search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('english', coalesce(name, '') || ' ' || coalesce(description, '') || ' ' || coalesce(sku, ''))
    ) STORED,
    
    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Exclude constraint (prevents overlapping periods)
    CONSTRAINT no_overlapping_availability 
        EXCLUDE USING GIST (item_id WITH =, availability_period WITH &&)
);

-- Create indexes on items
CREATE INDEX idx_items_owner ON app_data.items(owner_id);
CREATE INDEX idx_items_category ON app_data.items(category);
CREATE INDEX idx_items_sku ON app_data.items(sku);
CREATE INDEX idx_items_tags ON app_data.items USING GIN(tags);
CREATE INDEX idx_items_attributes ON app_data.items USING GIN(attributes);
CREATE INDEX idx_items_search ON app_data.items USING GIN(search_vector);
CREATE INDEX idx_items_availability ON app_data.items USING GIST(availability_period);

-- ============================================
-- Create Trigger Function for Updated_At
-- ============================================
CREATE OR REPLACE FUNCTION app_data.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply triggers
CREATE TRIGGER update_users_updated_at 
    BEFORE UPDATE ON app_data.users
    FOR EACH ROW
    EXECUTE FUNCTION app_data.update_updated_at_column();

CREATE TRIGGER update_items_updated_at 
    BEFORE UPDATE ON app_data.items
    FOR EACH ROW
    EXECUTE FUNCTION app_data.update_updated_at_column();

-- ============================================
-- Insert Sample Data
-- ============================================

-- Insert sample users
INSERT INTO app_data.users (email, username, password_hash, first_name, last_name, preferences, roles) VALUES
    ('john.doe@example.com', 'john_doe', '$2a$10$hash1', 'John', 'Doe', 
     '{"theme": "dark", "language": "en", "notifications": true}'::jsonb, 
     ARRAY['user', 'admin']),
    ('jane.smith@example.com', 'jane_smith', '$2a$10$hash2', 'Jane', 'Smith',
     '{"theme": "light", "language": "en", "emailNotifications": false}'::jsonb, 
     ARRAY['user', 'manager']),
    ('bob.wilson@example.com', 'bob_wilson', '$2a$10$hash3', 'Bob', 'Wilson',
     '{"theme": "dark", "language": "es"}'::jsonb, 
     ARRAY['user']);

-- Insert sample items
INSERT INTO app_data.items (owner_id, name, description, category, sku, price, cost, quantity, tags, attributes, availability_period, warehouse_location) VALUES
    (1, 'Laptop Pro 2026', 'High-performance laptop with 16GB RAM', 'electronics', 'ELEC-LAP-001', 
     1299.99, 899.99, 15,
     ARRAY['computer', 'portable', 'work', 'gaming'],
     '{"brand": "TechCorp", "ram": "16GB", "storage": "512GB SSD", "processor": "Intel i7", "warranty": "2 years"}'::jsonb,
     '[2026-01-01 00:00:00+00, 2026-12-31 23:59:59+00]'::tstzrange,
     POINT(10.5, 20.3)),
    
    (1, 'Wireless Mouse', 'Ergonomic wireless mouse', 'electronics', 'ELEC-MOU-001',
     29.99, 15.50, 50,
     ARRAY['computer', 'accessory', 'wireless'],
     '{"brand": "TechCorp", "connectivity": "Bluetooth", "battery": "AA"}'::jsonb,
     '[2026-01-01 00:00:00+00, infinity)'::tstzrange,
     POINT(10.5, 20.5)),
    
    (2, 'The Great Novel', 'Bestselling fiction book', 'books', 'BOOK-FIC-001',
     24.99, 12.00, 100,
     ARRAY['fiction', 'bestseller', 'hardcover'],
     '{"author": "Jane Author", "pages": 450, "publisher": "BookCorp", "isbn": "978-1234567890"}'::jsonb,
     '[2026-01-01 00:00:00+00, infinity)'::tstzrange,
     POINT(5.2, 15.8)),
    
    (3, 'Organic Coffee Beans', 'Premium organic coffee', 'food', 'FOOD-COF-001',
     18.99, 10.00, 200,
     ARRAY['organic', 'fair-trade', 'arabica'],
     '{"origin": "Colombia", "weight": "1kg", "roast": "medium"}'::jsonb,
     '[2026-01-01 00:00:00+00, 2026-06-30 23:59:59+00]'::tstzrange,
     POINT(3.0, 8.5));

-- ============================================
-- Sample Queries
-- ============================================

-- View all users with their item counts
SELECT 
    u.username,
    u.email,
    u.status,
    u.roles,
    COUNT(i.item_id) as item_count
FROM app_data.users u
LEFT JOIN app_data.items i ON u.user_id = i.owner_id
GROUP BY u.user_id, u.username, u.email, u.status, u.roles
ORDER BY item_count DESC;

-- Search items using full-text search
SELECT name, description, category, price
FROM app_data.items
WHERE search_vector @@ to_tsquery('english', 'laptop | computer');

-- Query JSON preferences
SELECT username, preferences->>'theme' as theme, preferences->>'language' as language
FROM app_data.users
WHERE preferences->>'theme' = 'dark';

-- Query array tags
SELECT name, tags, price
FROM app_data.items
WHERE 'computer' = ANY(tags);

-- Items currently available (using range types)
SELECT name, category, price, availability_period
FROM app_data.items
WHERE availability_period @> CURRENT_TIMESTAMP;

-- Window function: rank items by price within category
SELECT 
    name,
    category,
    price,
    RANK() OVER (PARTITION BY category ORDER BY price DESC) as price_rank_in_category,
    AVG(price) OVER (PARTITION BY category) as avg_category_price
FROM app_data.items
ORDER BY category, price_rank_in_category;

-- CTE example: Users with items worth more than average
WITH user_totals AS (
    SELECT 
        owner_id,
        SUM(price * quantity) as total_value
    FROM app_data.items
    GROUP BY owner_id
),
avg_value AS (
    SELECT AVG(total_value) as avg FROM user_totals
)
SELECT 
    u.username,
    ut.total_value,
    av.avg as average_value
FROM app_data.users u
JOIN user_totals ut ON u.user_id = ut.owner_id
CROSS JOIN avg_value av
WHERE ut.total_value > av.avg;
