using System.Text;
using System.Text.Json;
using ECommerceApp.Models;
using Microsoft.Extensions.Logging;

namespace ECommerceApp.Services;

public class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;
    private const string GatewayUrl = "https://payment-gateway.internal/api/charge";

    public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // No timeout set — HttpClient default is 100 seconds.
        // Under load the payment gateway regularly exceeds this, throwing TaskCanceledException
        // which surfaces as TimeoutException to callers.
        // Fix: set _httpClient.Timeout = TimeSpan.FromSeconds(10) and add Polly retry.
    }

    /// <summary>
    /// Charges the customer for a completed order.
    /// Calls the internal payment gateway over HTTP.
    ///
    /// Known issue (introduced v1.1.0):
    ///   No timeout is configured on the HttpClient.
    ///   When the gateway is slow under load, PostAsync hangs until the
    ///   100-second HttpClient default is reached, then throws TimeoutException.
    /// </summary>
    public async Task<PaymentResult> ChargeAsync(PaymentRequest request)
    {
        _logger.LogInformation(
            "Charging {Amount:C} for order {OrderId}",
            request.Amount, request.OrderId);

        var payload = JsonSerializer.Serialize(request);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // TimeoutException thrown here when gateway takes > 100s under load (line 28)
        var response = await _httpClient.PostAsync(GatewayUrl, content);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaymentResult>(body)!;

        _logger.LogInformation(
            "Payment {TransactionId} succeeded for order {OrderId}",
            result.TransactionId, request.OrderId);

        return result;
    }

    public async Task<IEnumerable<PaymentResult>> GetPaymentHistoryAsync(int customerId)
    {
        var response = await _httpClient.GetAsync($"{GatewayUrl}/history/{customerId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<PaymentResult>>(body)
               ?? Enumerable.Empty<PaymentResult>();
    }

    public async Task<bool> RefundAsync(string transactionId)
    {
        _logger.LogInformation("Refunding transaction {TransactionId}", transactionId);
        var response = await _httpClient.PostAsync(
            $"{GatewayUrl}/refund/{transactionId}", null);
        return response.IsSuccessStatusCode;
    }
}
