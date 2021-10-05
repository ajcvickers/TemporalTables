using System;
using System.Collections.Generic;

public class Customer
{
    public Guid Id { get; set; }
    public string Name  { get; set; }

    public List<Order> Orders { get; set; }
}