public class Customer
{
    public Customer(string name)
    {
        Name = name;
    }

    public Guid Id { get; private set; }
    public string Name  { get; init; }

    public List<Order> Orders { get; } = new List<Order>();
}