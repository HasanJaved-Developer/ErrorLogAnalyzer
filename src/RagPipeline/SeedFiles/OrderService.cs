using ECommerceApp.Models;
using ECommerceApp.Repositories;
using Microsoft.Extensions.Logging;

namespace ECommerceApp.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processes an order and applies a loyalty discount.
    ///
    /// CHANGE HISTORY:
    ///   v1.0.0 — included null guard: if (customer == null) return false;
    ///   v1.1.0 — null guard removed during refactor ("customers always exist in the DB").
    ///            This assumption breaks for soft-deleted customers whose orders are still active.
    /// </summary>
    public bool ProcessOrder(Order order)
    {
        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            order.Id, order.CustomerId);

        var customer = _customerRepository.GetById(order.CustomerId);
        // v1.0.0 had: if (customer == null) { _logger.LogWarning(...); return false; }
        // Removed in v1.1.0 — GetById returns null for soft-deleted customers.

        // NullReferenceException thrown here when customer is null (line 42)
        var discount = customer.LoyaltyTier switch
        {
            "Gold"     => 0.10m,
            "Platinum" => 0.20m,
            _          => 0.00m
        };

        var total = order.Amount * (1 - discount);

        _logger.LogInformation(
            "Order {OrderId} total after {Tier} discount: {Total}",
            order.Id, customer.LoyaltyTier, total);

        return _orderRepository.Save(order with { Total = total, CustomerName = customer.FullName });
    }

    public IEnumerable<Order> GetPendingOrders(string environment)
    {
        return _orderRepository.GetByStatus("pending", environment);
    }

    public bool CancelOrder(int orderId)
    {
        var order = _orderRepository.GetById(orderId);
        if (order == null)
        {
            _logger.LogWarning("Cancel requested for unknown order {OrderId}", orderId);
            return false;
        }

        if (order.Status == "shipped")
        {
            _logger.LogWarning("Cannot cancel shipped order {OrderId}", orderId);
            return false;
        }

        return _orderRepository.UpdateStatus(orderId, "cancelled");
    }
}
