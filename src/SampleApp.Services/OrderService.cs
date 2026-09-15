namespace SampleApp.Services;

public record OrderDto(int Id, string CustomerName, decimal Amount, string Status);

public interface IOrderService
{
    OrderDto? GetOrder(int id);
    decimal CalculateTax(decimal amount, decimal rate);
}

public class OrderService : IOrderService
{
    public OrderDto? GetOrder(int id)
    {
        if (id <= 0) return null;
        return new OrderDto(id, "Enterprise Corp", 1250.50m, "Processing");
    }

    public decimal CalculateTax(decimal amount, decimal rate)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative");
        if (rate < 0) throw new ArgumentOutOfRangeException(nameof(rate), "Rate cannot be negative");
        return Math.Round(amount * rate, 2);
    }
}
