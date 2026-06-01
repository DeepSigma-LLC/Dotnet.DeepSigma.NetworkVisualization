using DeepSigma.NetworkVisualization.Importers;

namespace DeepSigma.NetworkVisualization.Samples;

/// <summary>
/// Realistic domain hierarchy that exercises every behavior of <see cref="NetworkImporter.FromObject"/>:
/// nested objects, collections, primitives, dates, decimals, a Guid, an enum, a back-reference
/// (Order.Customer → Customer), and a null property (Address.Suite).
/// </summary>
public static class ObjectGraphSample
{
    public static Network Build()
    {
        var customer = new Customer
        {
            Id = Guid.Parse("9b0d2a1c-3f44-46aa-9d80-1d2e3f405060"),
            Name = "Alex Rivers",
            Tier = CustomerTier.Gold,
            JoinedOn = new DateTime(2019, 4, 12),
            PrimaryAddress = new Address
            {
                Street = "1280 Pine St",
                Suite = null,
                City = "Seattle",
                Region = "WA",
                Postal = "98101",
            },
        };
        var order1 = new Order
        {
            Id = 1001,
            PlacedOn = new DateTime(2026, 5, 14),
            Total = 142.95m,
            Customer = customer, // back-ref → cycle, importer should detect and emit a dashed "ref" edge
            Items =
            {
                new LineItem { Sku = "BK-001", Description = "Refactoring", Quantity = 1, UnitPrice = 39.95m },
                new LineItem { Sku = "BK-002", Description = "Clean Code",  Quantity = 1, UnitPrice = 34.00m },
                new LineItem { Sku = "MUG-01", Description = "Coffee Mug",  Quantity = 3, UnitPrice = 23.00m },
            },
        };
        var order2 = new Order
        {
            Id = 1002,
            PlacedOn = new DateTime(2026, 5, 28),
            Total = 18.00m,
            Customer = customer,
            Items =
            {
                new LineItem { Sku = "STK-01", Description = "Stickers",  Quantity = 6, UnitPrice = 3.00m },
            },
        };
        customer.Orders.Add(order1);
        customer.Orders.Add(order2);

        return NetworkImporter.FromObject(customer, new ObjectGraphOptions
        {
            MaxDepth = 6,
            RootLabel = "Customer",
        }) with { Title = "Object Graph: Customer" };
    }

    public sealed class Customer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public CustomerTier Tier { get; set; }
        public DateTime JoinedOn { get; set; }
        public Address? PrimaryAddress { get; set; }
        public List<Order> Orders { get; } = new();
    }

    public sealed class Address
    {
        public string Street { get; set; } = "";
        public string? Suite { get; set; }
        public string City { get; set; } = "";
        public string Region { get; set; } = "";
        public string Postal { get; set; } = "";
    }

    public sealed class Order
    {
        public int Id { get; set; }
        public DateTime PlacedOn { get; set; }
        public decimal Total { get; set; }
        public Customer? Customer { get; set; }
        public List<LineItem> Items { get; } = new();
    }

    public sealed class LineItem
    {
        public string Sku { get; set; } = "";
        public string Description { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public enum CustomerTier { Basic, Silver, Gold, Platinum }
}
