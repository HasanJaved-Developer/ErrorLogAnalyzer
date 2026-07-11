namespace AgentOrchestrator;

public enum LlmProvider { SelfHosted, Groq }

public record LlmConfig(LlmProvider Provider, string? ApiKey = null);
