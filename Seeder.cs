public class Seeder
{
    public static List<DateTime> MakeHistory()
    {
        using var context = new OrdersContext();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var timestamps = new List<DateTime>();

        Console.WriteLine("Making history...");

        // Add one customer and three products

        var customer = new Customer("Arthur");
        context.Customers.Add(customer);

        var products = new List<Product>
        {
            new("DeLorean", 1_000_000.00m),
            new("Flux Capacitor", 666.00m),
            new("Hoverboard", 59_000.00m)
        };

        context.AddRange(products);

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        // Wait a little bit, then change the price of the DeLorean
        Thread.Sleep(500);
        products[0].Price = 2_000_000.00m;
        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        // Wait a little bit, then change the price of the DeLorean
        Thread.Sleep(500);
        products[0].Price = 2_500_000.00m;
        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        // Wait a little bit, then create an order
        Thread.Sleep(500);
        context.Add(new Order(timestamps.Last()) { Customer = customer, Product = products[0] });
        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        // Wait a little bit, then change the price of the DeLorean
        Thread.Sleep(500);
        products[0].Price = 75_000.00m;
        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        // Wait a little bit, then change the price of the DeLorean
        Thread.Sleep(500);
        products[0].Price = 150_000.00m;
        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);

        Console.WriteLine("History made!");
        Console.WriteLine();

        return timestamps;
    }
}
