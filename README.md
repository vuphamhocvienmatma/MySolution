# Project Readme: .NET 9 Clean Architecture API

This document provides a comprehensive guide to the project's architecture, development workflows, and common services. It's intended for developers joining the project to get up to speed quickly.

---
## 1. Core Architecture 🏛️

This project is built upon **Clean Architecture** principles to create a decoupled, maintainable, and testable system. The core idea is that business logic should not depend on technical implementation details.

**The Dependency Rule:** `WebAPI` → `Infrastructure` → `Application` → `Domain`

![Clean Architecture Diagram](https://i.imgur.com/3n0n3t6.png)

### Project Layers
* **`Library`**: Centrailize Nuget Package
* **`Domain`**: The heart of the application. It contains the core business logic, entities, and domain events. It has **no dependencies** on other layers.
* **`Application`**: Contains the application's use cases, orchestrated via the **CQRS pattern** (Commands and Queries). It defines interfaces for infrastructure concerns but does not implement them.
* **`Infrastructure`**: Implements the interfaces defined in the `Application` layer. It contains all the technical details like database access (EF Core, Dapper), caching (Redis), external API calls (Flurl), and background job processing (Hangfire).
* **`WebAPI`**: The entry point. It exposes the application's features via a RESTful API. Controllers here are kept "thin" by simply receiving HTTP requests and dispatching commands or queries to MediatR.

### Key Technologies

| Area                  | Technology / Pattern                       |
| --------------------- | ------------------------------------------ |
| **Architecture** | Clean Architecture, DDD, CQRS              |
| **Database** | EF Core, Dapper, MongoDB                   |
| **Messaging** | MassTransit, RabbitMQ, Outbox Pattern      |
| **Caching** | Multi-Layer Cache (In-Memory + Redis)      |
| **Background Jobs** | Hangfire                                   |
| **API Security** | JWT, Rate Limiting, Multi-Tenancy          |
| **Logging** | Serilog with Correlation ID                |

---
## 2. Getting Started 🚀

### Prerequisites
* .NET 9 SDK
* Docker (for running database, Redis, RabbitMQ)
* A SQL Server instance

### Configuration
1.  Clone the repository.
2.  Open `src/WebAPI/appsettings.Development.json`.
3.  Update the `ConnectionStrings` for your SQL Server and Redis instances.
4.  Ensure all other settings (`JwtSettings`, `MessageBroker`, etc.) are configured as needed.

### Running the Project
```bash
# Navigate to the WebAPI project directory
cd src/WebAPI

# Run the application
dotnet run
