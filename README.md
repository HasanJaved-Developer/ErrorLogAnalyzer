# 🔍 Error Log Analyzer
[![Docker Compose CI](https://github.com/hasanjaved-developer/ErrorLogAnalyzer/actions/workflows/docker-compose-ci.yml/badge.svg)](https://github.com/hasanjaved-developer/ErrorLogAnalyzer/actions/workflows/docker-compose-ci.yml)
[![License](https://img.shields.io/badge/License-MIT-blue?logo=github)](LICENSE.txt)
[![Release](https://img.shields.io/badge/release-v1.0.0-blue)](https://github.com/hasanjaved-developer/ErrorLogAnalyzer/tags)
![Zero Windows Dependencies](https://img.shields.io/badge/Zero%20Windows%20Dependencies-Container%20Ready-blue?logo=linux)
[![GHCR api](https://img.shields.io/badge/ghcr.io-errorlog--analyzer%2Fapi-blue?logo=github)](https://ghcr.io/hasanjaved-developer/errorlog-analyzer/api)
[![GHCR web](https://img.shields.io/badge/ghcr.io-errorlog--analyzer%2Fweb-blue?logo=github)](https://ghcr.io/hasanjaved-developer/errorlog-analyzer/web)

### 🐳 Docker Hub Images

| Service | Pulls | Size | Version |
|----------|-------|------|----------|
| **API** | [![Pulls](https://img.shields.io/docker/pulls/hasanjaveddeveloper/errorlog-analyzer-api)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-api) | [![Size](https://img.shields.io/docker/image-size/hasanjaveddeveloper/errorlog-analyzer-api/v1.0.0)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-api/tags) | [![Version](https://img.shields.io/docker/v/hasanjaveddeveloper/errorlog-analyzer-api?sort=semver)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-api/tags) |
| **Web (Portal)** | [![Pulls](https://img.shields.io/docker/pulls/hasanjaveddeveloper/errorlog-analyzer-web)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-web) | [![Size](https://img.shields.io/docker/image-size/hasanjaveddeveloper/errorlog-analyzer-web/v1.0.0)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-web/tags) | [![Version](https://img.shields.io/docker/v/hasanjaveddeveloper/errorlog-analyzer-web?sort=semver)](https://hub.docker.com/r/hasanjaveddeveloper/errorlog-analyzer-web/tags) |

An AI-powered root cause analysis tool built entirely in **.NET 9 / C#**. It connects an agentic LLM to your error logs and deployment history, retrieves relevant source code via a RAG pipeline, and returns a plain-English hypothesis explaining **why an error occurred**.

> ⚠️ **Disclaimer:** This tool provides a *hypothesis* for root cause analysis — not a definitive answer. LLMs reason over the provided context and may produce plausible but incorrect conclusions. **All suggestions must be verified by a developer before acting on them.** AI is a fast first draft — the human engineer is always the final authority.

---

## 🧠 What Problem Does It Solve?

When a production error appears, a developer typically has to:

1. Open the error log manually
2. Check recent deployments and find which files changed
3. Read through those files to find the suspicious code
4. Form a hypothesis about the root cause

This tool **automates steps 1–3** and gives the LLM enough context to assist with step 4 — reducing a 30-minute investigation to a few seconds.

---

## 🏗️ Architecture

```
Developer
    │
    ▼
┌──────────────────────────────┐
│  Web UI (ASP.NET Core MVC)   │   http://localhost:7000
│  choose provider: Self-hosted│ 
│  or Groq (+ API key)         │
└────────────┬─────────────────┘
             │  POST /api/analyze
             ▼
┌─────────────────────────────┐
│   API Layer (ASP.NET Core)  │   http://localhost:5000
└────────────┬────────────────┘
             ▼
┌─────────────────────────────────────────────────────┐
│   Agent Orchestrator (Microsoft.Extensions.AI)      │
│   LLM decides which MCP tools to call, RAG supplies │
│   matching code context                             │
└─────────────────────────────────────────────────────┘
```

### Agentic RAG Analysis Flow

<img src="docs/Agentic%20RAG%20Analysis%20Flow.png" alt="Agentic RAG Analysis Flow" width="500">

### RAG Seed Service (startup indexing)

<img src="docs/RAG%20Seed%20Service.png" alt="RAG Seed Service" width="500">

Source `.mmd` diagrams: [`docs/Agentic RAG Analysis Flow.mmd`](docs/Agentic%20RAG%20Analysis%20Flow.mmd), [`docs/RAG Seed Service.mmd`](docs/RAG%20Seed%20Service.mmd).

---

## ⚙️ Tech Stack

| Role | Technology |
|---|---|
| LLM Inference (self-hosted) | Ollama + `qwen2.5:3b` (OpenAI-compatible API) — no GPU means high latency |
| LLM Inference (cloud, optional) | Groq — requires an API key from [console.groq.com](https://console.groq.com/), sent with every request |
| Embeddings | nomic-embed-text via Ollama |
| Vector Store | Qdrant (Docker container) |
| MCP Server | .NET Console App (custom, stdio JSON protocol) |
| API Layer | ASP.NET Core — `/api/analyze` |
| Web UI | ASP.NET Core MVC — provider/API key selection |
| LLM Client | Microsoft.Extensions.AI |
| Database | SQL Server (ErrorLog + DeploymentLog) |
| Language | C# · .NET 9 |

---

## 🔧 MCP Server Tools

The MCP Server exposes two tools that the LLM agent can call autonomously:

### `get_error_logs(from, to, errorType)`
Queries the `ErrorLog` table, grouped by error type and stack trace, and returns the occurrence count, latest stack trace, and latest occurrence timestamp per group.

### `get_recent_deployments(env, date)`
Queries the `DeploymentLog` table and returns the two deployments closest to a given timestamp, including version, commit hash, and the list of changed files (deserialized from `DeploymentLog.FilesChanged`).

---

## 🤖 Agentic Loop — Example Questions

- "Summarize all production errors from the last 3 days."
- "Why is `PaymentService.ChargeAsync` timing out?"
- "What changed in production in the last few days that might have caused new errors?"

The agent decides **which tools to call and in what order** based on the question — no hardcoded flow.

---

## 📁 Repository Structure

```
src/
├── ErrorAnalyserApi/     # ASP.NET Core — /api/analyze endpoint
├── ApiIntegrationMvc/    # Web UI — calls the API
├── McpServer/            # .NET Console App — exposes 2 MCP tools
├── RagPipeline/          # Chunker, embedder, Qdrant store, seed-time indexing
├── AgentOrchestrator/    # Drives the agentic loop (Microsoft.Extensions.AI)
├── DataAccess/           # EF Core: ErrorLog + DeploymentLog + migrations + seeding
├── docker-compose.yml            # Build from source
├── docker-compose.dhub.yml       # Prebuilt images from Docker Hub (tested)
├── docker-compose.ghcr.yml       # Prebuilt images from GitHub Container Registry
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

```bash
# Option 1 — build everything from source
docker-compose up -d

# Option 2 — pull tested prebuilt images from Docker Hub
docker-compose -f docker-compose.dhub.yml up -d
```

On first startup:
1. The database is created, migrated, and seeded with sample `ErrorLog` and `DeploymentLog` data.
2. The RAG pipeline chunks and embeds the seed source files referenced by those deployments into Qdrant.

Expected answer time: **10–25 seconds** with a GPU, **60–180 seconds** CPU-only (self-hosted). Using Groq is much faster but requires an API key.

## 🌐 Running Services

| Service         | URL                              | Description                        |
|-----------------|-----------------------------------|------------------------------------|
| Web UI          | http://localhost:7000            | Frontend application               |
| REST API        | http://localhost:5000/index.html | Backend API                        |
| Qdrant Dashboard| http://localhost:6333/dashboard  | Vector database UI                 |
| Ollama          | http://localhost:11434           | Local LLM inference engine         |

---

## ⚠️ Known Limitations / Production Considerations

### RAG Indexing Happens Once, at Startup

RAG indexing (chunking, embedding, and vector storage) does **not** happen at query time. It runs once at first startup (`RagSeedService`), indexing every file referenced by seeded deployments into Qdrant. At query time, RAG only performs retrieval — embedding the prompt and searching Qdrant for the closest matching chunks — which is fast.

In a production system, indexing should instead trigger automatically (**.NET `BackgroundService`**) whenever a new deployment is recorded, so new files are pre-indexed before any developer query arrives.

### LLM Hallucination Risk

The LLM may produce a plausible but incorrect root cause hypothesis. Always treat the output as a **starting point for investigation**, not a confirmed diagnosis.

### No Authentication

The `/api/analyze` endpoint has no authentication in this demo. A production deployment should secure it appropriately.

---

## 📚 Concepts Demonstrated

- **Agentic AI** — LLM autonomously decides which tools to call based on the question
- **MCP (Model Context Protocol)** — Standardized tool interface between LLM and backend
- **RAG (Retrieval-Augmented Generation)** — Indexing once at startup, then retrieving only the relevant code chunks at query time
- **Microsoft.Extensions.AI** — Provider-agnostic .NET abstraction for LLM + tool calling, swappable between a self-hosted Ollama model and Groq
- **Self-hosted or cloud LLM** — Runs fully offline by default, or via Groq's API when a key is supplied

---

## 📄 License

MIT License — free to use, modify, and distribute.

---

*Built with .NET 9 · C# · Ollama · Qdrant · Microsoft.Extensions.AI*
