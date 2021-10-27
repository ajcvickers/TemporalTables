public class Order
{
    public Order(DateTime orderDate)
    {
        OrderDate = orderDate;
    }

    public Guid Id { get; private set; }
    public DateTime OrderDate { get; init; }

    public Product Product { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}