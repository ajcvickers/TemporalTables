public class Product
{
    public Product(string name, decimal price)
    {
        Name = name;
        Price = price;
    }

    public Guid Id { get; private set; }
    public string Name { get; init; }
    public decimal Price { get; set; }
}