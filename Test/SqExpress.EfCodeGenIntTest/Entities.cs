using System;
using System.Collections.Generic;

namespace SqExpress.EfCodeGenIntTest;

public sealed class Customer
{
    public int CustomerId { get; set; }

    public int? ParentCustomerId { get; set; }

    public Guid PublicId { get; set; }

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal? CreditLimit { get; set; }

    public double Score { get; set; }

    public byte RiskLevel { get; set; }

    public short LegacyCode { get; set; }

    public long ExternalNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public byte[]? BinaryCode { get; set; }

    public byte[] FixedBinaryCode { get; set; } = Array.Empty<byte>();

    public string? MetadataXml { get; set; }

    public TimeSpan? SessionTimeout { get; set; }

    public List<Order> Orders { get; } = new List<Order>();
}

public sealed class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = "";

    public List<Product> Products { get; } = new List<Product>();
}

public sealed class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string Sku { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public bool IsDiscontinued { get; set; }

    public Category Category { get; set; } = null!;

    public List<OrderLine> OrderLines { get; } = new List<OrderLine>();
}

public sealed class Order
{
    public long OrderId { get; set; }

    public int CustomerId { get; set; }

    public string OrderNumber { get; set; } = "";

    public DateTime CreatedUtc { get; set; }

    public decimal Total { get; set; }

    public Customer Customer { get; set; } = null!;

    public List<OrderLine> Lines { get; } = new List<OrderLine>();
}

public sealed class OrderLine
{
    public long OrderId { get; set; }

    public short LineNo { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

public sealed class AuditLog
{
    public long AuditLogId { get; set; }

    public string EntityName { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime CreatedUtc { get; set; }
}
