using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static void Main()
    {
        var timestamps = Seed(new TimeSpan(0, 0, 5));
        
        LookupPrices("DeLorean", timestamps[1], DateTime.UtcNow.AddDays(2));
        
        FindOrder("Arthur", timestamps[3]);
        
        DeleteCustomer("Arthur");
        
        QueryCustomerAndOrderSnapshots();
        
        RestoreCustomer("Arthur");
        
        QueryCustomerAndOrderSnapshots();
    }

    private static void LookupPrices(string productName, DateTime from, DateTime to)
    {
        using var context = new OrdersContext();

        var product = context.Products.Single(product => product.Name == productName);
        
        Console.WriteLine($"The '{product.Name}' with PK {product.Id} is currently ${product.Price}.");
        Console.WriteLine($"  Historical prices from {from} to {to}:");

        var productSnapshots = context.Products
            .TemporalBetween(from, to)
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
                $"    The '{snapshot.Product.Name}' with PK {snapshot.Product.Id} was ${snapshot.Product.Price} from {snapshot.PeriodStart} until {snapshot.PeriodEnd}.");
        }

        Console.WriteLine();
    }

    private static void FindOrder(string customerName, DateTime on)
    {
        using var context = new OrdersContext(log: true);

        var order = context.Orders
            .TemporalAsOf(on)
            .Include(e => e.Product)
            .Include(e => e.Customer)
            .Single(order =>
                order.Customer.Name == customerName
                && order.OrderDate > on.Date
                && order.OrderDate < on.Date.AddDays(1));

        Console.WriteLine();

        Console.WriteLine(
            $"{order.Customer.Name} ordered a {order.Product.Name} for ${order.Product.Price} on {order.OrderDate}");

        Console.WriteLine();
    }

    private static void DeleteCustomer(string customerName)
    {
        using var context = new OrdersContext();

        var customer = context.Customers
            .Include(e => e.Orders)
            .Single(customer => customer.Name == customerName);
        
        context.RemoveRange(customer.Orders);
        context.Remove(customer);
        context.SaveChanges();
    }

    private static void RestoreCustomer(string customerName)
    {
        using var context = new OrdersContext(log: true);

        var customerDeletedOn = context.Customers
            .TemporalAll()
            .Where(customer => customer.Name == customerName)
            .OrderBy(customer => EF.Property<DateTime>(customer, "PeriodEnd"))
            .Select(customer => EF.Property<DateTime>(customer, "PeriodEnd"))
            .Last();
        
        Console.WriteLine();

        var customerAndOrders = context.Customers
            .TemporalAsOf(customerDeletedOn.AddMilliseconds(-1))
            .Include(e => e.Orders)
            .Single();
        
        Console.WriteLine();

        context.Add(customerAndOrders);
        context.SaveChanges();
        
        Console.WriteLine();
    }

    private static void QueryCustomerAndOrderSnapshots()
    {
        using var context = new OrdersContext(log: true);

        var customerSnapshots = context.Customers
            .TemporalAll()
            .OrderBy(customer => EF.Property<DateTime>(customer, "PeriodStart"))
            .Select(customer =>
                new
                {
                    Customer = customer,
                    PeriodStart = EF.Property<DateTime>(customer, "PeriodStart"),
                    PeriodEnd = EF.Property<DateTime>(customer, "PeriodEnd")
                })
            .ToList();

        foreach (var snapshot in customerSnapshots)
        {
            Console.WriteLine(
                $"The customer '{snapshot.Customer.Name}' existed from {snapshot.PeriodStart} until {snapshot.PeriodEnd}.");
        }

        Console.WriteLine();

        var orderSnapshots = context.Orders
            .TemporalAll()
            .OrderBy(order => EF.Property<DateTime>(order, "PeriodStart"))
            .Select(order =>
                new
                {
                    Order = order,
                    PeriodStart = EF.Property<DateTime>(order, "PeriodStart"),
                    PeriodEnd = EF.Property<DateTime>(order, "PeriodEnd")
                })
            .ToList();

        foreach (var snapshot in orderSnapshots)
        {
            Console.WriteLine(
                $"The order with ID '{snapshot.Order.Id}' existed from {snapshot.PeriodStart} until {snapshot.PeriodEnd}.");
        }

        Console.WriteLine();
    }

    private static List<DateTime> Seed(TimeSpan sleep)
    {
        using var context = new OrdersContext();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        
        var timestamps = new List<DateTime>();

        var customer = new Customer { Name = "Arthur" };
        context.Customers.Add(customer);

        var products = new List<Product>
        {
            new() { Name = "DeLorean", Price = 1_000_000.00m },
            new() { Name = "Flux Capacitor", Price = 666.00m },
            new() { Name = "Hoverboard", Price = 59_000.00m }
        };

        context.AddRange(products);

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);

        products[0].Price = 2_000_000.00m;

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);

        products[0].Price = 2_500_000.00m;

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);
        
        context.Add(new Order { Customer = customer, Product = products[0], OrderDate = timestamps.Last() });

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);

        products[0].Price = 75_000.00m;

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);
        
        products[0].Price = 150_000.00m;

        context.SaveChanges();
        timestamps.Add(DateTime.UtcNow);
        Thread.Sleep(sleep);

        return timestamps;
    }
}