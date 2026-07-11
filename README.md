# 🔍 Error Log Analyzer

An AI-powered root cause analysis tool built entirely in **.NET 9 / C#**. It connects an agentic LLM to your error logs and deployment history, retrieves relevant source code via a RAG pipeline, and returns a plain-English hypothesis explaining **why an error occurred**.

> ⚠️ **Disclaimer:** This tool provides a *hypothesis* for root cause analysis — not a definitive answer. LLMs reason over the provided context and may produce plausible but incorrect conclusions. **All suggestions must be verified by a developer before acting on them.** AI is a fast first draft — the human engineer is always the final authority.

---

## 🧠 What Problem Does It Solve?

When a production error appears, a developer typically has to:

1. Open the error log manually
2. Check recent deployments
3. Find which files were changeds
4. Read through those files to find the suspicious code
5. Form a hypothesis about the root cause

This tool **automates steps 1–4** and gives the LLM enough context to assist with step 5 — reducing a 30-minute investigation to a few seconds.

---

## 🏗️ Architecture

```
Developer
    │
    │  POST /api/analyze
    │  "What caused the timeout error after yesterday's deployment?"
    ▼
┌─────────────────────────────┐
│   API Layer (ASP.NET Core)  │
│   /api/analyze endpoint     │
└────────────┬────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│           Agent Orchestrator (Semantic Kernel)                          │
│                                                                         │
│  LLM decides which MCP tools to call                                    │
│                                                                         │
│  ┌──────────────────┐ ┌─────────────────────┐ ┌──────────────────────┐  │
│  │get_error_logs()  │ │get_changed_files()  │ │get_recent_deployments│  │
│  └────────┬─────────┘ └──────────┬──────────┘ └──────────┬───────────┘  │
│           │                      │                       │              │
│  ┌────────▼──────────────────────▼───────────────────────▼────────────┐ │
│  │                  SQL Server / Oracle Database                      │ │
│  │              ErrorLog + DeploymentLog Tables                       │ │
│  └────────────────────────────────┬───────────────────────────────────┘ │
│                                   │                                     │
│              ┌────────────────────┘─────────────────────────┐           │
│              │  get_error_frequency() also queries ErrorLog │           │
│              └──────────────────────────────────────────────┘           │
└────────────────────────┬────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────┐
│                  RAG Pipeline                       │
│                                                     │
│  Changed files → Chunker → Embedder → Qdrant        │
│                               (nomic-embed-text)    │
│                                                     │
│  Stack trace → Semantic Search → Top-K Chunks       │
└────────────────────────┬────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────┐
│           LLM Inference (Ollama + Llama 3.1)        │
│                                                     │
│  error details + deployment info + code chunks      │
│                    ↓                                │
│       Root cause hypothesis + confidence level      │
└─────────────────────────────────────────────────────┘
```

---

## ⚙️ Tech Stack

| Role | Technology |
|---|---|
| LLM Inference | Ollama + Llama 3.1 (self-hosted, OpenAI-compatible API) |
| Embeddings | nomic-embed-text via Ollama |
| Vector Store | Qdrant (Docker container) |
| MCP Server | .NET Console App (custom, stdio JSON protocol) |
| API Layer | ASP.NET Core — `/api/analyze` |
| LLM Client | Semantic Kernel (Microsoft) |
| Database | SQL Server (ErrorLog + DeploymentLog) |
| Language | C# · .NET 9 |

Everything runs **self-hosted** — no OpenAI API key required, no cloud dependency.

---

## 🔧 MCP Server Tools

The MCP Server exposes four tools that the LLM agent can call autonomously:

### `get_error_logs(from, to, type)`
Queries the `ErrorLog` table and returns error type, message, stack trace, and first occurrence timestamp within the specified time range.

### `get_recent_deployments(env, date)`
Queries the `DeploymentLog` table and returns deployments closest to a given timestamp, including version number and commit hash.

### `get_changed_files(deployment_id)`
Returns the list of source files changed in a specific deployment, stored in `DeploymentLog.files_changed` as JSON.

### `get_error_frequency(error_type)`
Queries the `ErrorLog` table and returns the monthly occurrence count of a given error type across the past 12 months. This allows the LLM to distinguish between a **new error** (introduced by a recent deployment) and a **recurring bug** (pre-existing issue unrelated to recent changes).

---

## 🤖 Agentic Loop — Example Questions

### Q1: "What caused the timeout error after yesterday's deployment?"
```
get_recent_deployments()
    → get_changed_files(deployment_id)
        → get_error_logs()
            → RAG retrieval (relevant code chunks)
                → LLM answer
```

### Q2: "Is this NullReferenceException a recurring issue?"
```
get_error_frequency("NullReferenceException")
    → LLM answer
```

### Q3: "Summarize all errors in production this week"
```
get_error_logs(this_week)
    → LLM answer
```

The agent decides **which tools to call and in what order** based on the question — no hardcoded flow.

---

## 📁 Repository Structure

```
error-log-analyzer/
├── src/
│   ├── Api/                  # ASP.NET Core — /api/analyze endpoint
│   ├── McpServer/            # .NET Console App — exposes 4 MCP tools
│   ├── RagPipeline/          # Chunker, Embedder, Retriever
│   ├── LlmClient/            # Calls Ollama via Semantic Kernel
│   ├── AgentOrchestrator/    # Drives the agentic loop
│   └── DataAccess/           # SQL queries for ErrorLog + DeploymentLog
├── database/
│   ├── schema.sql            # ErrorLog + DeploymentLog table definitions
│   └── seed.sql              # Realistic sample data to run immediately
├── tests/                    # Unit + integration tests
├── docker-compose.yml        # Qdrant + Ollama setup
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

# Everything spins up via Docker
docker-compose up -d

On first startup the API will:
1. Create the SQL Server database and apply EF migrations
2. Seed `ErrorLog` and `DeploymentLog` with realistic demo data
3. Index the seed source files into Qdrant via the RAG pipeline

Expected answer time: **10–25 seconds** with a GPU, **60–180 seconds** CPU-only.

## 🌐 Running Services

Once `docker compose up` completes, the following services will be available:

| Service         | URL                              | Description                        |
|-----------------|----------------------------------|------------------------------------|
| Web UI          | http://localhost:7000            | Frontend application               |
| REST API        | http://localhost:5000/index.html | Backend API                        |
| Qdrant Dashboard| http://localhost:6333/dashboard  | Vector database UI                 |
| Ollama          | http://localhost:11434           | Local LLM inference engine         |

---

## ⚠️ Known Limitations / Production Considerations

### RAG Indexing is Synchronous (Demo Only)

In this demo, RAG indexing (chunking, embedding, and vector storage) happens **synchronously at query time** — meaning the developer waits while files are chunked and embedded before getting a response.

In a production system, this should be moved to a **.NET `BackgroundService`** that triggers automatically when a new deployment is recorded in `DeploymentLog`, so vectors are **pre-indexed** before any developer query arrives. The query-time RAG step would then only perform retrieval — which is fast.

### LLM Hallucination Risk

The LLM may produce a plausible but incorrect root cause hypothesis. Always treat the output as a **starting point for investigation**, not a confirmed diagnosis.

### No Authentication

The `/api/analyze` endpoint has no authentication in this demo. A production deployment should secure it appropriately.

---

## 📚 Concepts Demonstrated

- **Agentic AI** — LLM autonomously decides which tools to call based on the question
- **MCP (Model Context Protocol)** — Standardized tool interface between LLM and backend
- **RAG (Retrieval-Augmented Generation)** — Retrieving only the relevant code chunks instead of sending entire files
- **Semantic Kernel** — Microsoft's .NET library for LLM + tool calling integration
- **Self-hosted LLM** — Entire stack runs offline with no external API dependency

---

## 📄 License

MIT License — free to use, modify, and distribute.

---

*Built with .NET 9 · C# · Ollama · Qdrant · Semantic Kernel*
