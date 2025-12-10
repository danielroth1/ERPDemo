namespace InventoryManagement.Models;

public static class SeedData
{
    public static List<ProductSeed> GetSampleProducts()
    {
        return new List<ProductSeed>
        {
            // Electronics
            new ProductSeed { Name = "Wireless Bluetooth Headphones", Description = "Premium noise-cancelling over-ear headphones", CategoryName = "Electronics", Price = 89.99m, Sku = "ELEC-001" },
            new ProductSeed { Name = "USB-C Charging Cable", Description = "Fast charging USB-C cable, 6ft length", CategoryName = "Electronics", Price = 12.99m, Sku = "ELEC-002" },
            new ProductSeed { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse with adjustable DPI", CategoryName = "Electronics", Price = 29.99m, Sku = "ELEC-003" },
            new ProductSeed { Name = "Mechanical Keyboard", Description = "RGB backlit mechanical gaming keyboard", CategoryName = "Electronics", Price = 79.99m, Sku = "ELEC-004" },
            new ProductSeed { Name = "4K Webcam", Description = "High-definition webcam with auto-focus", CategoryName = "Electronics", Price = 149.99m, Sku = "ELEC-005" },
            new ProductSeed { Name = "Portable SSD 1TB", Description = "External solid state drive with USB 3.2", CategoryName = "Electronics", Price = 119.99m, Sku = "ELEC-006" },
            new ProductSeed { Name = "Smart Watch", Description = "Fitness tracker with heart rate monitor", CategoryName = "Electronics", Price = 199.99m, Sku = "ELEC-007" },
            new ProductSeed { Name = "Wireless Earbuds", Description = "True wireless earbuds with charging case", CategoryName = "Electronics", Price = 59.99m, Sku = "ELEC-008" },
            new ProductSeed { Name = "Phone Stand", Description = "Adjustable aluminum phone stand", CategoryName = "Electronics", Price = 19.99m, Sku = "ELEC-009" },
            new ProductSeed { Name = "Power Bank 20000mAh", Description = "High-capacity portable charger", CategoryName = "Electronics", Price = 45.99m, Sku = "ELEC-010" },
            
            // Clothing
            new ProductSeed { Name = "Classic White T-Shirt", Description = "100% cotton comfortable t-shirt", CategoryName = "Clothing", Price = 24.99m, Sku = "CLTH-001" },
            new ProductSeed { Name = "Blue Denim Jeans", Description = "Slim fit denim jeans", CategoryName = "Clothing", Price = 69.99m, Sku = "CLTH-002" },
            new ProductSeed { Name = "Hoodie Sweatshirt", Description = "Warm fleece-lined hoodie", CategoryName = "Clothing", Price = 54.99m, Sku = "CLTH-003" },
            new ProductSeed { Name = "Running Shoes", Description = "Lightweight athletic running shoes", CategoryName = "Clothing", Price = 89.99m, Sku = "CLTH-004" },
            new ProductSeed { Name = "Winter Jacket", Description = "Insulated waterproof winter jacket", CategoryName = "Clothing", Price = 149.99m, Sku = "CLTH-005" },
            new ProductSeed { Name = "Cotton Socks (3-Pack)", Description = "Comfortable cotton blend socks", CategoryName = "Clothing", Price = 14.99m, Sku = "CLTH-006" },
            new ProductSeed { Name = "Baseball Cap", Description = "Adjustable cotton baseball cap", CategoryName = "Clothing", Price = 22.99m, Sku = "CLTH-007" },
            new ProductSeed { Name = "Leather Belt", Description = "Genuine leather dress belt", CategoryName = "Clothing", Price = 34.99m, Sku = "CLTH-008" },
            new ProductSeed { Name = "Yoga Pants", Description = "High-waist stretch yoga pants", CategoryName = "Clothing", Price = 44.99m, Sku = "CLTH-009" },
            new ProductSeed { Name = "Sunglasses", Description = "UV protection polarized sunglasses", CategoryName = "Clothing", Price = 39.99m, Sku = "CLTH-010" },

            // Home & Kitchen
            new ProductSeed { Name = "Coffee Maker", Description = "Programmable 12-cup coffee maker", CategoryName = "Home & Kitchen", Price = 79.99m, Sku = "HOME-001" },
            new ProductSeed { Name = "Blender", Description = "High-speed blender for smoothies", CategoryName = "Home & Kitchen", Price = 64.99m, Sku = "HOME-002" },
            new ProductSeed { Name = "Non-Stick Pan Set", Description = "3-piece non-stick frying pan set", CategoryName = "Home & Kitchen", Price = 49.99m, Sku = "HOME-003" },
            new ProductSeed { Name = "Air Fryer", Description = "Digital air fryer with temperature control", CategoryName = "Home & Kitchen", Price = 99.99m, Sku = "HOME-004" },
            new ProductSeed { Name = "Knife Set", Description = "Professional 8-piece knife set with block", CategoryName = "Home & Kitchen", Price = 89.99m, Sku = "HOME-005" },
            new ProductSeed { Name = "Dish Towels (6-Pack)", Description = "Absorbent cotton dish towels", CategoryName = "Home & Kitchen", Price = 19.99m, Sku = "HOME-006" },
            new ProductSeed { Name = "Food Storage Containers", Description = "BPA-free plastic containers with lids", CategoryName = "Home & Kitchen", Price = 29.99m, Sku = "HOME-007" },
            new ProductSeed { Name = "Electric Kettle", Description = "Fast-boiling stainless steel kettle", CategoryName = "Home & Kitchen", Price = 44.99m, Sku = "HOME-008" },
            new ProductSeed { Name = "Toaster", Description = "2-slice toaster with bagel setting", CategoryName = "Home & Kitchen", Price = 34.99m, Sku = "HOME-009" },
            new ProductSeed { Name = "Cutting Board Set", Description = "Bamboo cutting boards (3 sizes)", CategoryName = "Home & Kitchen", Price = 24.99m, Sku = "HOME-010" },

            // Books
            new ProductSeed { Name = "The Great Novel", Description = "Bestselling fiction novel", CategoryName = "Books", Price = 16.99m, Sku = "BOOK-001" },
            new ProductSeed { Name = "Learn Programming", Description = "Complete guide to modern programming", CategoryName = "Books", Price = 34.99m, Sku = "BOOK-002" },
            new ProductSeed { Name = "Cooking Masterclass", Description = "Professional cooking techniques book", CategoryName = "Books", Price = 29.99m, Sku = "BOOK-003" },
            new ProductSeed { Name = "Mystery Thriller", Description = "Suspenseful page-turner mystery", CategoryName = "Books", Price = 14.99m, Sku = "BOOK-004" },
            new ProductSeed { Name = "Science Fiction Epic", Description = "Award-winning sci-fi trilogy", CategoryName = "Books", Price = 39.99m, Sku = "BOOK-005" },
            new ProductSeed { Name = "Self-Help Guide", Description = "Personal development and motivation", CategoryName = "Books", Price = 19.99m, Sku = "BOOK-006" },
            new ProductSeed { Name = "History of Technology", Description = "Comprehensive tech history book", CategoryName = "Books", Price = 27.99m, Sku = "BOOK-007" },
            new ProductSeed { Name = "Photography Basics", Description = "Beginner's guide to photography", CategoryName = "Books", Price = 24.99m, Sku = "BOOK-008" },
            new ProductSeed { Name = "Gardening Encyclopedia", Description = "Complete gardening reference guide", CategoryName = "Books", Price = 32.99m, Sku = "BOOK-009" },
            new ProductSeed { Name = "Travel Adventures", Description = "Inspiring travel stories and guides", CategoryName = "Books", Price = 22.99m, Sku = "BOOK-010" },

            // Sports & Outdoors
            new ProductSeed { Name = "Yoga Mat", Description = "Non-slip exercise yoga mat with bag", CategoryName = "Sports & Outdoors", Price = 29.99m, Sku = "SPRT-001" },
            new ProductSeed { Name = "Dumbbell Set", Description = "Adjustable weight dumbbell set", CategoryName = "Sports & Outdoors", Price = 89.99m, Sku = "SPRT-002" },
            new ProductSeed { Name = "Resistance Bands", Description = "Set of 5 resistance bands", CategoryName = "Sports & Outdoors", Price = 24.99m, Sku = "SPRT-003" },
            new ProductSeed { Name = "Water Bottle", Description = "Insulated stainless steel water bottle", CategoryName = "Sports & Outdoors", Price = 19.99m, Sku = "SPRT-004" },
            new ProductSeed { Name = "Camping Tent", Description = "4-person waterproof camping tent", CategoryName = "Sports & Outdoors", Price = 149.99m, Sku = "SPRT-005" },
            new ProductSeed { Name = "Sleeping Bag", Description = "All-season sleeping bag", CategoryName = "Sports & Outdoors", Price = 59.99m, Sku = "SPRT-006" },
            new ProductSeed { Name = "Hiking Backpack", Description = "40L hiking backpack with rain cover", CategoryName = "Sports & Outdoors", Price = 79.99m, Sku = "SPRT-007" },
            new ProductSeed { Name = "Jump Rope", Description = "Speed jump rope for cardio", CategoryName = "Sports & Outdoors", Price = 12.99m, Sku = "SPRT-008" },
            new ProductSeed { Name = "Foam Roller", Description = "High-density foam roller for recovery", CategoryName = "Sports & Outdoors", Price = 34.99m, Sku = "SPRT-009" },
            new ProductSeed { Name = "Sports Towel", Description = "Quick-dry microfiber sports towel", CategoryName = "Sports & Outdoors", Price = 14.99m, Sku = "SPRT-010" },

            // Beauty & Personal Care
            new ProductSeed { Name = "Face Moisturizer", Description = "Hydrating facial moisturizer SPF 30", CategoryName = "Beauty & Personal Care", Price = 29.99m, Sku = "BEAU-001" },
            new ProductSeed { Name = "Shampoo & Conditioner Set", Description = "Natural ingredients hair care set", CategoryName = "Beauty & Personal Care", Price = 24.99m, Sku = "BEAU-002" },
            new ProductSeed { Name = "Electric Toothbrush", Description = "Rechargeable sonic toothbrush", CategoryName = "Beauty & Personal Care", Price = 49.99m, Sku = "BEAU-003" },
            new ProductSeed { Name = "Hair Dryer", Description = "Professional ionic hair dryer", CategoryName = "Beauty & Personal Care", Price = 69.99m, Sku = "BEAU-004" },
            new ProductSeed { Name = "Makeup Brush Set", Description = "12-piece professional makeup brushes", CategoryName = "Beauty & Personal Care", Price = 34.99m, Sku = "BEAU-005" },
            new ProductSeed { Name = "Body Lotion", Description = "Moisturizing body lotion with vitamin E", CategoryName = "Beauty & Personal Care", Price = 16.99m, Sku = "BEAU-006" },
            new ProductSeed { Name = "Perfume", Description = "Luxury eau de parfum 100ml", CategoryName = "Beauty & Personal Care", Price = 89.99m, Sku = "BEAU-007" },
            new ProductSeed { Name = "Nail Polish Set", Description = "10-color nail polish collection", CategoryName = "Beauty & Personal Care", Price = 19.99m, Sku = "BEAU-008" },
            new ProductSeed { Name = "Facial Cleanser", Description = "Gentle foaming face wash", CategoryName = "Beauty & Personal Care", Price = 14.99m, Sku = "BEAU-009" },
            new ProductSeed { Name = "Hair Styling Gel", Description = "Strong hold hair styling gel", CategoryName = "Beauty & Personal Care", Price = 12.99m, Sku = "BEAU-010" },

            // Toys & Games
            new ProductSeed { Name = "Board Game Classic", Description = "Family board game for all ages", CategoryName = "Toys & Games", Price = 29.99m, Sku = "TOYS-001" },
            new ProductSeed { Name = "Puzzle 1000 Pieces", Description = "Scenic landscape jigsaw puzzle", CategoryName = "Toys & Games", Price = 19.99m, Sku = "TOYS-002" },
            new ProductSeed { Name = "Building Blocks Set", Description = "Creative building blocks 500 pieces", CategoryName = "Toys & Games", Price = 44.99m, Sku = "TOYS-003" },
            new ProductSeed { Name = "Remote Control Car", Description = "High-speed RC racing car", CategoryName = "Toys & Games", Price = 59.99m, Sku = "TOYS-004" },
            new ProductSeed { Name = "Action Figure", Description = "Collectible superhero action figure", CategoryName = "Toys & Games", Price = 24.99m, Sku = "TOYS-005" },
            new ProductSeed { Name = "Playing Cards", Description = "Premium quality playing card deck", CategoryName = "Toys & Games", Price = 9.99m, Sku = "TOYS-006" },
            new ProductSeed { Name = "Chess Set", Description = "Wooden chess set with folding board", CategoryName = "Toys & Games", Price = 34.99m, Sku = "TOYS-007" },
            new ProductSeed { Name = "Stuffed Animal", Description = "Soft plush teddy bear", CategoryName = "Toys & Games", Price = 19.99m, Sku = "TOYS-008" },
            new ProductSeed { Name = "Art Supplies Kit", Description = "Complete drawing and painting set", CategoryName = "Toys & Games", Price = 39.99m, Sku = "TOYS-009" },
            new ProductSeed { Name = "Musical Instrument Toy", Description = "Kids keyboard with learning mode", CategoryName = "Toys & Games", Price = 49.99m, Sku = "TOYS-010" },

            // Office Supplies
            new ProductSeed { Name = "Notebook Set", Description = "Pack of 5 lined notebooks", CategoryName = "Office Supplies", Price = 14.99m, Sku = "OFFC-001" },
            new ProductSeed { Name = "Pen Pack", Description = "Blue ink ballpoint pens (12 pack)", CategoryName = "Office Supplies", Price = 9.99m, Sku = "OFFC-002" },
            new ProductSeed { Name = "Desk Organizer", Description = "Bamboo desktop organizer with drawers", CategoryName = "Office Supplies", Price = 29.99m, Sku = "OFFC-003" },
            new ProductSeed { Name = "Sticky Notes", Description = "Colorful sticky note pads variety pack", CategoryName = "Office Supplies", Price = 12.99m, Sku = "OFFC-004" },
            new ProductSeed { Name = "Stapler", Description = "Heavy-duty desktop stapler", CategoryName = "Office Supplies", Price = 16.99m, Sku = "OFFC-005" },
            new ProductSeed { Name = "Paper Clips", Description = "Jumbo paper clips box of 1000", CategoryName = "Office Supplies", Price = 7.99m, Sku = "OFFC-006" },
            new ProductSeed { Name = "File Folders", Description = "Letter size file folders (25 pack)", CategoryName = "Office Supplies", Price = 19.99m, Sku = "OFFC-007" },
            new ProductSeed { Name = "Desk Lamp", Description = "LED desk lamp with USB charging port", CategoryName = "Office Supplies", Price = 39.99m, Sku = "OFFC-008" },
            new ProductSeed { Name = "Calculator", Description = "Scientific calculator for students", CategoryName = "Office Supplies", Price = 24.99m, Sku = "OFFC-009" },
            new ProductSeed { Name = "Whiteboard", Description = "Magnetic dry erase whiteboard", CategoryName = "Office Supplies", Price = 44.99m, Sku = "OFFC-010" }
        };
    }
}

public class ProductSeed
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty;
}
