using Microsoft.AspNetCore.Mvc;
using ApiIntegrationMvc.Models;

namespace ApiIntegrationMvc.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IActionResult Index() => View(new AnalyzeViewModel());

    [HttpPost]
    public async Task<IActionResult> Analyze(AnalyzeViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Question))
        {
            ModelState.AddModelError(nameof(model.Question), "Please enter a question.");
            return View("Index", model);
        }

        if (model.Provider == "Groq" && string.IsNullOrWhiteSpace(model.ApiKey))
        {
            ModelState.AddModelError(nameof(model.ApiKey), "An API key is required for Groq.");
            return View("Index", model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AnalyzerApi");
            var response = await client.PostAsJsonAsync("/api/analyze", new
            {
                question = model.Question,
                provider = model.Provider,
                apiKey   = model.ApiKey
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>();
                model.Answer = result?.Answer ?? "(No answer returned)";
            }
            else
            {
                model.Error = $"API error {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the Analyzer API.");
            model.Error = "The Analyzer API is not reachable. Ensure the Api project is running on the configured base URL.";
        }

        return View("Index", model);
    }
}
