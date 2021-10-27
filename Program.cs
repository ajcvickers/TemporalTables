Console.WriteLine("EF Core 6.0 Temporal Tables...");
Console.WriteLine();

var timestamps = Seeder.MakeHistory();

LookupCurrentPrice("DeLorean");

LookupPrices("DeLorean", timestamps[1], timestamps[3]);

FindOrder("Arthur", timestamps[3]);

/// <summary>
///    Use a normal EF query to lookup the current price of a product.
/// </summary>
static void LookupCurrentPrice(string productName)
{
    using var context = new OrdersContext();

    var product = context.Products.Single(product => product.Name == productName);

    Console.WriteLine($"The '{product.Name}' with PK {product.Id} is currently ${product.Price}.");

    Console.WriteLine();
}

/// <summary>
///     Use a temporal query to lookup historical prices of a product.
/// </summary>
static void LookupPrices(string productName, DateTime from, DateTime to)
{
    using var context = new OrdersContext();

    Console.WriteLine($"Historical prices for {productName} from {from} to {to}:");

    var productSnapshots = context.Products
        .TemporalAll()
        .OrderBy(product => EF.Property<DateTime>(product, "PeriodStart"))
        .Where(product => product.Name == productName)
        .Select(product =>
            new
            {
                Product = product,
                PeriodStart = EF.Property<DateTime>(product, "PeriodStart"),
                PeriodEnd = EF.Property<DateTime>(product, "PeriodEnd")
            })
        .ToList();

    foreach (var snapshot in productSnapshots)
    {
        Console.WriteLine(
            $"  The '{snapshot.Product.Name}' with PK {snapshot.Product.Id} was ${snapshot.Product.Price} from {snapshot.PeriodStart} until {snapshot.PeriodEnd}.");
    }

    Console.WriteLine();
}

/// <summary>
///     Find an order and its related product and customer at a specific time in the past.
/// </summary>
static void FindOrder(string customerName, DateTime on)
{
    using var context = new OrdersContext();

    var order = context.Orders
        .TemporalAsOf(on)
        .Include(e => e.Product)
        .Include(e => e.Customer)
        .Single(order => order.Customer.Name == customerName);

    Console.WriteLine(
        $"{order.Customer.Name} ordered a {order.Product.Name} for ${order.Product.Price} on {order.OrderDate}");

    Console.WriteLine();
}
