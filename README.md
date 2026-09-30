# 🤖 AI Help Desk & Knowledge Management Platform

<p align="center">
  <strong>AI-powered customer support and knowledge management backend built with ASP.NET Core 10</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core" />
  <img src="https://img.shields.io/badge/Entity%20Framework%20Core-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="EF Core" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Ollama-Local%20AI-black?style=for-the-badge" alt="Ollama" />
  <img src="https://img.shields.io/badge/JWT-Authentication-orange?style=for-the-badge" alt="JWT" />
  <img src="https://img.shields.io/badge/xUnit-Testing-5E5E5E?style=for-the-badge" alt="xUnit" />
  <img src="https://img.shields.io/badge/Tests-194%20Passing-success?style=for-the-badge" alt="Tests" />
</p>

---

## 📌 Overview

**AI Help Desk & Knowledge Management Platform** is an interview-focused backend application built using **ASP.NET Core 10**.

The application combines a traditional customer support ticketing system with AI-assisted functionality, including:

* 🎫 Ticket management
* 👥 Customer and support-agent workflows
* 🔐 JWT authentication and authorization
* 🔄 Refresh token rotation
* 🤖 AI-powered ticket analysis
* 🧠 Text embeddings
* 🔎 Semantic search
* 🔀 Hybrid search
* 📚 Knowledge-base management
* 🧩 Retrieval-Augmented Generation (RAG)
* 🧪 Automated unit, service, controller, and integration tests

The project uses **Ollama** for local AI inference, allowing the AI functionality to run without depending on an external AI API.

> 💡 **Core Design Principle:**
> AI provides suggestions and generated content, while application and domain rules remain responsible for validation, authorization, and business decisions.

---

# ✨ Features

## 🎫 Help Desk

* Create support tickets
* View tickets
* Assign tickets to support agents
* Categorize tickets
* Update ticket status
* Manage ticket priority
* Add ticket comments
* Retrieve ticket comments
* Customer-specific ticket access
* Support-agent-specific ticket access
* Administrator access

### 🎯 Ticket Status

```text
🟢 Open
   ↓
🔵 InProgress
   ↓
🟡 Resolved
   ↓
⚫ Closed
```

Tickets can also be reopened according to the application's domain rules.

---

## 👥 User Roles

The application supports three primary roles:

| Role                | Responsibilities                              |
| ------------------- | --------------------------------------------- |
| 👤 Customer         | Create and view their own tickets             |
| 🧑‍💻 Support Agent | Manage assigned tickets and support customers |
| 👑 Admin            | Administrative and broader system access      |

Authorization is implemented using both:

* 🔐 Role-based authorization
* 🛡️ Resource-level authorization

A valid JWT alone does not automatically grant access to another user's resources.

---

# 🔐 Authentication & Security

The application uses:

* ASP.NET Core Identity
* JWT Bearer authentication
* Short-lived access tokens
* Refresh tokens
* Refresh token rotation
* Refresh token families
* Role claims
* Resource-level authorization
* Password hashing through ASP.NET Core Identity
* Global exception handling
* ASP.NET Core User Secrets for local sensitive configuration

---

## 🔄 Refresh Token Flow

```text
                🔑 Login
                   │
                   ▼
          ┌─────────────────┐
          │  Authentication │
          └────────┬────────┘
                   │
          ┌────────┴────────┐
          ▼                 ▼
    🎟️ Access Token    🔄 Refresh Token
                              │
                              ▼
                           Refresh
                              │
                     ┌────────┴────────┐
                     ▼                 ▼
              New Access Token   New Refresh Token
```

The previous refresh token is revoked after successful rotation.

Refresh token families are tracked so that token reuse can be detected.

---

# 🤖 AI Capabilities

The application integrates AI through an abstraction:

```text
              ┌───────────────┐
              │   IAiService  │
              └───────┬───────┘
                      │
                      ▼
            ┌──────────────────┐
            │ OllamaAiService  │
            └────────┬─────────┘
                     │
                     ▼
                  🦙 Ollama
```

This keeps the application layer independent from the specific AI provider.

---

## 🧠 AI Ticket Analysis

The AI can analyze tickets and provide:

### 🏷️ Category Suggestion

Supported categories:

```text
🔐 Account & Login
💳 Billing
🏨 Booking
💬 General Inquiry
🔧 Technical Issue
```

### 🚦 Priority Suggestion

```text
🟢 Low
🟡 Medium
🟠 High
🔴 Critical
```

### 📝 Ticket Summary

The AI can generate a concise summary of the customer's issue.

### 💬 Suggested Customer Response

The AI can generate a professional response that can be reviewed before being used.

---

# 🛡️ AI Does Not Bypass Business Rules

One of the important architectural decisions in this project is that **AI is not treated as the source of truth**.

```text
              🎫 Ticket
                  │
                  ▼
             🤖 AI Analysis
                  │
                  ▼
           💡 AI Suggestions
                  │
                  ▼
        ┌─────────────────────┐
        │ Application Rules   │
        │ & Validation        │
        └──────────┬──────────┘
                   │
                   ▼
             🧠 Domain Rules
                   │
                   ▼
             💾 State Change
```

For example, an AI-generated priority must still be validated against the application's supported priority values before changing the ticket.

This prevents unexpected AI output from directly modifying domain state.

---

# 🦙 Ollama Integration

The application uses **Ollama** for local AI inference.

### 💬 Chat Model

```text
llama3.2
```

### 🧠 Embedding Model

```text
nomic-embed-text
```

### 🌐 Default Ollama Endpoint

```text
http://localhost:11434
```

The application communicates with Ollama through `HttpClient`.

---

# 📚 Knowledge Management

The knowledge base allows support teams to maintain reusable support information.

A knowledge article contains:

* 🆔 ID
* 📝 Title
* 📄 Content
* ✅ Active/Inactive status
* 📅 Created timestamp
* 🔄 Updated timestamp
* 🧠 Embedding

Supported operations include:

* ➕ Create article
* ✏️ Update article
* 🔍 Search articles
* 🧠 Semantic search
* 🔀 Hybrid search
* ✅ Activate article
* 🚫 Deactivate article
* 🤖 Ask questions using RAG

---

# 🧠 Embeddings

Knowledge articles are converted into numerical vectors using:

```text
nomic-embed-text
```

The process is:

```text
📚 Knowledge Article
        │
        ▼
🦙 Embedding Model
        │
        ▼
🔢 Numerical Vector
```

When a user performs semantic search, the user's question is also converted into an embedding.

The application compares the vectors using **cosine similarity**.

---

# 🔎 Semantic Search

Semantic search allows the system to find articles based on meaning rather than only exact keywords.

```text
❓ User Question
       │
       ▼
🧠 Generate Embedding
       │
       ▼
🔢 Compare Article Vectors
       │
       ▼
📐 Cosine Similarity
       │
       ▼
📊 Rank Results
```

The vector similarity implementation also handles:

* ⚠️ Dimension mismatches
* 0️⃣ Zero vectors

### ⚠️ Embedding Model Changes

Embeddings generated by different models may have different vector dimensions.

Therefore, when changing the embedding model, existing knowledge articles should be **re-embedded** rather than mixing vectors generated by different models.

---

# 🔀 Hybrid Search

The application combines keyword and semantic search.

```text
        🔤 Keyword Search
                │
                │
                ├──────────┐
                │          │
                ▼          ▼
        🧠 Semantic Search
                │
                ▼
          🔀 Hybrid Search
                │
                ▼
          📊 Ranked Results
```

### 🔤 Keyword Search

Useful when exact terms are important.

### 🧠 Semantic Search

Useful when the user's wording differs from the wording used in the article.

### 🔀 Hybrid Search

Combines both approaches to provide more flexible retrieval.

---

# 🧩 Retrieval-Augmented Generation (RAG)

The application implements RAG for knowledge-base question answering.

### 🔄 RAG Pipeline

```text
❓ User Question
       │
       ▼
🔀 Hybrid Search
       │
       ▼
📚 Retrieve Relevant Articles
       │
       ▼
🎯 Relevance Filtering
       │
       ▼
🔝 Select Top Results
       │
       ▼
📄 Build Context
       │
       ▼
🤖 Send Context + Question to AI
       │
       ▼
💬 Generate Answer
```

The current relevance threshold is:

```text
0.5
```

Up to **3 relevant knowledge articles** are used as context.

The AI prompt instructs the model to answer using the supplied knowledge-base information.

If relevant information cannot be found, the application returns a fallback response rather than allowing the AI to invent an answer.

---

# 🏗️ Architecture

The application follows a layered architecture.

```text
┌───────────────────────────────────────────────┐
│                 🌐 API Layer                  │
│        Controllers / Middleware / HTTP        │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│             ⚙️ Application Layer              │
│     DTOs / Services / Interfaces / Mappings  │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│                🧠 Domain Layer                │
│          Entities / Enums / Rules             │
└───────────────────────────────────────────────┘

┌───────────────────────────────────────────────┐
│            🔧 Infrastructure Layer            │
│ Identity / EF Core / JWT / Ollama / DB       │
└───────────────────────────────────────────────┘
```

---

# 📂 Project Structure

```text
AIHelpDesk/
│
├── 📁 src/
│   │
│   ├── 📁 AIHelpDesk.API/
│   │   ├── 📁 Controllers/
│   │   ├── 📁 Middleware/
│   │   └── 📄 Program.cs
│   │
│   ├── 📁 AIHelpDesk.Application/
│   │   ├── 📁 DTOs/
│   │   ├── 📁 Interfaces/
│   │   ├── 📁 Services/
│   │   ├── 📁 Mappings/
│   │   └── 📄 DependencyInjection.cs
│   │
│   ├── 📁 AIHelpDesk.Domain/
│   │   ├── 📁 Entities/
│   │   └── 📁 Enums/
│   │
│   └── 📁 AIHelpDesk.Infrastructure/
│       ├── 📁 Identity/
│       ├── 📁 Persistence/
│       ├── 📁 Authentication/
│       ├── 📁 AI/
│       └── 📄 DependencyInjection.cs
│
├── 📁 tests/
│   └── 📁 AIHelpDesk.Tests/
│
├── 📄 AIHelpDesk.slnx
├── 📄 .gitignore
└── 📄 README.md
```

---

# 🔗 Dependency Direction

```text
        🌐 API
          │
          ▼
    ⚙️ Application
          │
          ▼
       🧠 Domain

🔧 Infrastructure
       │
       ├──────────► ⚙️ Application
       │
       └──────────► 🧠 Domain

🧪 Tests
       │
       ├──────────► 🌐 API
       ├──────────► ⚙️ Application
       └──────────► 🔧 Infrastructure
```

The Domain layer does not depend on Infrastructure.

This keeps the core business rules isolated from external technologies.

---

# 🧱 Domain Model

Main entities:

```text
🎫 Ticket
💬 TicketComment
🏷️ Category
📚 KnowledgeArticle
🔄 RefreshToken
👤 ApplicationUser
```

The `Ticket` entity uses private setters and domain methods such as:

```text
AssignTo()
AssignCategory()
StartProgress()
Resolve()
Close()
Reopen()
SetPriority()
```

This ensures ticket state transitions are controlled by domain rules.

---

# 🌐 API Endpoints

## 🔐 Authentication

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
```

---

## 🎫 Tickets

```http
GET    /api/tickets
GET    /api/tickets/{id}
GET    /api/tickets/assigned
GET    /api/tickets/unassigned
POST   /api/tickets
PATCH  /api/tickets/{id}/status
PATCH  /api/tickets/{id}/assign
```

---

## 📚 Knowledge Base

```http
POST   /api/knowledgearticles
POST   /api/knowledgearticles/ask
GET    /api/knowledgearticles
GET    /api/knowledgearticles/{id}
GET    /api/knowledgearticles/search
GET    /api/knowledgearticles/semantic-search
GET    /api/knowledgearticles/hybrid-search
PUT    /api/knowledgearticles/{id}
PATCH  /api/knowledgearticles/{id}/activate
PATCH  /api/knowledgearticles/{id}/deactivate
```

---

# ⚠️ Global Exception Handling

The application uses a global exception handler for consistent API error responses.

```text
ArgumentException
      │
      ▼
400 Bad Request
```

```text
KeyNotFoundException
      │
      ▼
404 Not Found
```

```text
InvalidOperationException
      │
      ▼
409 Conflict
```

```text
Unexpected Exception
      │
      ▼
500 Internal Server Error
```

Unexpected errors return generic messages rather than exposing internal exception details.

---

# 🧪 Testing Strategy

The project contains multiple levels of automated testing.

## 🔬 Service Tests

Business logic is tested independently of the API.

Examples include:

* Ticket operations
* Category operations
* Knowledge article operations
* AI suggestion application
* Authentication
* Refresh token behavior
* Ticket comments

---

## 🎮 Controller Tests

Controller tests verify:

* HTTP status codes
* Authorization
* Validation
* Service interaction
* Successful requests
* Failure scenarios

---

## 🔗 Integration Tests

Integration tests use:

```text
ASP.NET Core WebApplicationFactory
              +
SQLite In-Memory Database
```

This allows the actual ASP.NET Core application pipeline to be tested across multiple layers.

SQL Server is used for normal application development while SQLite is used for integration testing.

---

# 📊 Test Results

Current project test status:

```text
╔══════════════════════════════╗
║ 🧪 Total Tests      : 194    ║
║ ✅ Passed           : 194    ║
║ ❌ Failed           : 0      ║
║ ⏭️ Skipped          : 0      ║
║ ⚠️ Build Warnings   : 0      ║
╚══════════════════════════════╝
```

---

# 💡 Important Engineering Decisions

## 🤖 1. Fake AI → Ollama

The project initially used a fake AI implementation.

This allowed business logic and tests to be developed without requiring an AI server.

After the application logic was stable, the implementation was moved to Ollama for local inference.

---

## 🦙 2. Local AI with Ollama

External AI API usage introduced quota limitations during development.

Ollama was selected for local development so that AI inference could run locally without relying on an external API quota.

---

## 🧠 3. Fake Embeddings → Real Embeddings

The initial development implementation used fake embeddings.

The final implementation uses:

```text
nomic-embed-text
```

When switching embedding implementations, existing vectors need to be regenerated to ensure vector dimensions and embedding semantics remain compatible.

---

## 📐 4. RAG Threshold Tuning

The initial semantic relevance threshold was too strict.

A threshold of:

```text
1.0
```

resulted in relevant documents being filtered out.

The threshold was adjusted to:

```text
0.5
```

based on the observed similarity scores from the embedding model.

---

## 🏷️ 5. Exact AI Category Matching

The AI initially returned category names that were semantically similar but did not exactly match the application's categories.

The AI prompt was changed to constrain output to the application's supported category names.

---

## 🚦 6. Exact Priority Validation

Priority validation initially relied on partial string matching.

This could allow an invalid value containing a valid priority name to pass.

Validation was changed to require an exact match against the supported priority values.

---

## 🗄️ 7. SQL Server + SQLite Compatibility

The application uses SQL Server while integration tests use SQLite.

A SQL Server-specific embedding column configuration caused SQLite tests to fail.

The embedding configuration was therefore changed to remain provider-neutral.

---

## 🔐 8. Resource-Level Authorization

Role-based authorization alone was not sufficient.

The application also verifies whether the authenticated user owns or is assigned to the requested resource.

This provides an additional authorization boundary beyond simply checking whether the user has a valid JWT.

---

# 🛠️ Development & Debugging Lessons

Several real development issues were identified and resolved during implementation.

| Issue                                  | Resolution                                 |
| -------------------------------------- | ------------------------------------------ |
| External AI quota limitation           | Moved to local Ollama                      |
| Fake/real embedding dimension mismatch | Re-embedding strategy                      |
| RAG threshold too strict               | Adjusted threshold to `0.5`                |
| AI category mismatch                   | Constrained AI output                      |
| Weak priority validation               | Exact enum validation                      |
| SQL Server-specific EF configuration   | Provider-neutral configuration             |
| SQL Server/SQLite provider conflict    | Removed incompatible provider registration |
| Integration JWT configuration          | Added test JWT configuration               |
| Incorrect route constraint             | Corrected route syntax                     |
| Incorrect `CreatedAtAction` target     | Changed to correct GET endpoint            |
| Authorization test failures            | Corrected test preconditions               |
| Nullable compiler warnings             | Corrected nullable contracts               |
| OpenAPI package vulnerability warning  | Updated package version                    |
| Test fixture warning                   | Corrected `IDisposable` handling           |

These debugging experiences are also useful examples of practical backend development and troubleshooting.

---

# 🔒 Security

The application implements:

* 🔐 ASP.NET Core Identity
* 🔑 JWT authentication
* 🔄 Refresh token rotation
* 👥 Role-based authorization
* 🛡️ Resource-level authorization
* 🔒 Password hashing
* 🚫 Generic production error responses
* 🔐 User Secrets for local sensitive configuration
* 🎟️ Token family tracking
* ♻️ Refresh token reuse detection

Sensitive secrets are not committed to the repository.

---

# ⚙️ Configuration

The application uses configuration for:

* 🗄️ SQL Server connection string
* 🔐 JWT settings
* 🦙 Ollama settings

Sensitive JWT signing keys should be stored using ASP.NET Core User Secrets during local development.

Example:

```powershell
dotnet user-secrets set "JwtSettings:SecretKey" "your-development-secret"
```

> ⚠️ Never commit production secrets, passwords, API keys, or private connection strings to Git.

---

# 🚀 Getting Started

## 📋 Prerequisites

Install:

* [.NET 10 SDK](https://dotnet.microsoft.com/)
* SQL Server / LocalDB
* [Ollama](https://ollama.com/)

---

## 🦙 Install Ollama Models

Pull the chat model:

```powershell
ollama pull llama3.2
```

Pull the embedding model:

```powershell
ollama pull nomic-embed-text
```

Start Ollama:

```powershell
ollama serve
```

---

# 📥 Clone the Repository

```powershell
git clone https://github.com/sai-abhiroop/AIHelpDesk.git
cd AIHelpDesk
```

---

# 🔐 Configure User Secrets

Navigate to the API project:

```powershell
cd src/AIHelpDesk.API
```

Set the JWT signing key:

```powershell
dotnet user-secrets set "JwtSettings:SecretKey" "your-development-secret"
```

Configure the remaining local settings according to `appsettings.json`.

---

# 🏗️ Build the Application

From the repository root:

```powershell
dotnet build
```

---

# ▶️ Run the API

```powershell
dotnet run --project src/AIHelpDesk.API
```

When running in the Development environment, OpenAPI and Scalar are available for API exploration.

---

# 🧪 Run Tests

From the repository root:

```powershell
dotnet test
```

Expected result:

```text
194 passed
0 failed
0 skipped
```

---

# 📖 API Documentation

The API exposes OpenAPI documentation and Scalar during Development.

This provides an interactive interface for exploring and testing API endpoints.

---

# 📈 Production Considerations

The current project is designed as an interview-level backend application.

Potential production improvements include:

### 🔐 Security

* Hash refresh tokens at rest
* Handle concurrent refresh-token rotation
* Stronger startup configuration validation
* Production secret management

### 🧠 AI / Search

* Native vector storage
* Dedicated vector database
* More advanced hybrid-search weighting
* AI retry and resilience policies
* AI response evaluation and monitoring

### ⚡ Performance

* Distributed caching where appropriate
* Background processing for expensive AI operations
* More efficient vector retrieval at scale

### 📊 Operations

* Health checks
* Centralized logging
* Application monitoring
* Distributed tracing
* Containerized deployment
* CI/CD pipeline
* Database migration automation

---

# 🎯 What This Project Demonstrates

This project demonstrates practical experience with:

### 💻 Backend Development

* C#
* ASP.NET Core Web API
* REST APIs
* Dependency Injection
* SOLID principles
* Layered architecture
* DTOs
* Middleware

### 🗄️ Data Access

* Entity Framework Core
* SQL Server
* SQLite
* Entity relationships
* Database configuration
* Read-only query optimization

### 🔐 Security

* ASP.NET Core Identity
* JWT authentication
* Refresh tokens
* Token rotation
* Role-based authorization
* Resource-level authorization

### 🤖 Artificial Intelligence

* Ollama
* LLM integration
* Prompt engineering
* AI output validation
* Text embeddings
* Cosine similarity
* Semantic search
* Hybrid search
* RAG

### 🧪 Testing

* xUnit
* Moq
* Unit testing
* Service testing
* Controller testing
* Integration testing
* WebApplicationFactory
* SQLite integration testing

### 🛠️ Engineering

* Debugging
* Error handling
* Configuration management
* Git
* GitHub
* API documentation

---

# 📊 Project Status

| Component                  | Status            |
| -------------------------- | ----------------- |
| 🌐 ASP.NET Core API        | ✅ Complete        |
| 🎫 Ticket Management       | ✅ Complete        |
| 👥 User Roles              | ✅ Complete        |
| 🔐 JWT Authentication      | ✅ Complete        |
| 🔄 Refresh Token Rotation  | ✅ Complete        |
| 🛡️ Resource Authorization | ✅ Complete        |
| 🤖 AI Integration          | ✅ Complete        |
| 🦙 Ollama Integration      | ✅ Complete        |
| 🧠 Embeddings              | ✅ Complete        |
| 🔎 Semantic Search         | ✅ Complete        |
| 🔀 Hybrid Search           | ✅ Complete        |
| 🧩 RAG                     | ✅ Complete        |
| 📚 Knowledge Base          | ✅ Complete        |
| 🧪 Automated Tests         | ✅ Complete        |
| 📊 Test Coverage           | 194 Passing Tests |
| ⚠️ Build Warnings          | 0                 |

---

# 🏆 Final Summary

**AI Help Desk & Knowledge Management Platform** demonstrates how a traditional ASP.NET Core backend can be extended with practical AI capabilities without allowing AI to bypass core application rules.

The project combines:

```text
🌐 ASP.NET Core
       +
🧠 Domain & Business Logic
       +
🔐 Authentication & Authorization
       +
🗄️ Entity Framework Core
       +
🦙 Local AI with Ollama
       +
🔢 Embeddings
       +
🔎 Semantic Search
       +
🔀 Hybrid Search
       +
🧩 RAG
       +
🧪 Automated Testing
       =
🚀 AI-Powered Help Desk Platform
```

---

## 👨‍💻 Author

**Abhiroop**

Built as an interview-focused ASP.NET Core project demonstrating backend development, security, testing, and practical AI integration.