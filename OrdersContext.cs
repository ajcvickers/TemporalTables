using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class OrdersContext : DbContext
{
    private readonly bool _log;

    public OrdersContext()
    {
    }

    public OrdersContext(bool log)
    {
        _log = log;
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Orders");

        if (_log)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Product>()
            .Property(e => e.Price)
            .HasPrecision(18, 2);
        
        modelBuilder
            .Entity<Customer>()
            .ToTable("Customers", b => b.IsTemporal());
        
        modelBuilder
            .Entity<Product>()
            .ToTable("Products", b => b.IsTemporal());
        
        modelBuilder
            .Entity<Order>()
            .ToTable("Orders", b => b.IsTemporal());
    }
}