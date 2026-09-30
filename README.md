# EmployeeManager

A simple **ASP.NET Core Web API** for managing employees and departments. The application follows a layered architecture using Entity Framework Core and SQL Server.

The application is designed to run locally during development and deploy to **Microsoft Azure**, with:

* **Azure App Service** hosting the ASP.NET Core Web API
* **Azure SQL Database** hosting application data
* **Entity Framework Core** for database access and migrations
* **Swagger/OpenAPI** for API documentation and testing

---

## Architecture

```text
                         ┌──────────────────────┐
                         │      Client / UI      │
                         └──────────┬───────────┘
                                    │ HTTP/HTTPS
                                    ▼
                         ┌──────────────────────┐
                         │   Azure App Service  │
                         │  EmployeeManager.API │
                         └──────────┬───────────┘
                                    │
                                    ▼
                    ┌────────────────────────────┐
                    │ EmployeeManager.Application│
                    │    Application Services     │
                    └─────────────┬──────────────┘
                                  │
                                  ▼
                    ┌────────────────────────────┐
                    │ EmployeeManager.Infrastructure│
                    │ EF Core / AppDbContext       │
                    └─────────────┬──────────────┘
                                  │
                              SQL/TLS
                                  │
                                  ▼
                    ┌────────────────────────────┐
                    │      Azure SQL Database    │
                    │        db_employee         │
                    └────────────────────────────┘
```

---

## Solution Structure

```text
EmployeeManager/
│
├── EmployeeManager.API/
│   ├── Controllers/
│   ├── Properties/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
│
├── EmployeeManager.Application/
│   └── Application services / business logic
│
├── EmployeeManager.Core/
│   └── Domain models / entities
│
├── EmployeeManager.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Migrations/
│   └── EF Core configuration
│
├── EmployeeManager.Tests/
│   └── Unit tests
│
├── EmployeeManagerApi.IntegrationTests/
│   └── Integration tests
│
└── EmployeeManager.sln
```

### Projects

| Project                               | Purpose                                                       |
| ------------------------------------- | ------------------------------------------------------------- |
| `EmployeeManager.API`                 | ASP.NET Core Web API, controllers and application entry point |
| `EmployeeManager.Application`         | Application logic and services                                |
| `EmployeeManager.Core`                | Domain entities and core business models                      |
| `EmployeeManager.Infrastructure`      | Entity Framework Core, database access and migrations         |
| `EmployeeManager.Tests`               | Unit tests                                                    |
| `EmployeeManagerApi.IntegrationTests` | API integration tests                                         |

---

# Technology Stack

* **.NET 10**
* **ASP.NET Core Web API**
* **Entity Framework Core**
* **Microsoft SQL Server**
* **Azure SQL Database**
* **Azure App Service**
* **Swagger / OpenAPI**
* **xUnit** for testing
* **Visual Studio / VS Code**
* **SQL Server Management Studio (SSMS)**

---

# Prerequisites

## Local Development

Install:

* [.NET 10 SDK](https://dotnet.microsoft.com/)
* Visual Studio 2022/2026 or VS Code
* SQL Server / SQL Server LocalDB
* SQL Server Management Studio (SSMS) or Azure Data Studio
* Git

Optional:

```powershell
dotnet tool install --global dotnet-ef
```

Verify the .NET installation:

```powershell
dotnet --version
```

---

# Database Configuration

The application uses the connection string named:

```text
EmployeeDB
```

## Local Development

For local development, the application can use SQL Server LocalDB.

Example:

```json
{
  "ConnectionStrings": {
    "EmployeeDB": "Server=(localdb)\\MSSQLLocalDB;Database=db_employee;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  }
}
```

This configuration is intended for **local development only**.

Do not use LocalDB as the production database for the Azure deployment.

---

# Azure Production Database

The production application uses **Azure SQL Database**.

The general architecture is:

```text
Azure App Service
       │
       │ SQL connection
       ▼
Azure SQL Database
       │
       └── db_employee
```

The Azure SQL connection string should be configured as an **App Service Application Setting**, rather than committing production credentials to Git.

A typical Azure SQL connection string looks like:

```text
Server=tcp:<server-name>.database.windows.net,1433;
Initial Catalog=db_employee;
Persist Security Info=False;
User ID=<username>;
Password=<password>;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

Replace the placeholders with the values from your Azure SQL Database.

> **Important:** Do not commit passwords, database credentials, API keys, or other secrets to `appsettings.json` or source control.

---

# Create the Database Locally

From the repository root:

```powershell
dotnet ef database update `
  --project EmployeeManager.Infrastructure `
  --startup-project EmployeeManager.API
```

Alternatively, use Visual Studio **Package Manager Console**.

Set:

```text
Default project: EmployeeManager.Infrastructure
```

Then run:

```powershell
Update-Database -Project EmployeeManager.Infrastructure -StartupProject EmployeeManager.API
```

The migrations will create the database and apply the configured schema and seed data.

---

# Seed Data

The application uses Entity Framework Core's `HasData(...)` functionality to seed initial Departments and Employees.

The seed configuration is located in:

```text
EmployeeManager.Infrastructure
    └── Data
        └── AppDbContext.cs
```

When the migrations are applied, the configured seed data will be inserted into the database.

---

# Running the API Locally

From the repository root:

```powershell
dotnet run --project EmployeeManager.API
```

The console will display the configured Kestrel URL, for example:

```text
Now listening on: http://localhost:5000
```

Open the displayed URL in a browser or API client.

When running in Development, Swagger/OpenAPI can be accessed through:

```text
/swagger
```

For example:

```text
https://localhost:<port>/swagger
```

Use the actual port displayed by the application.

---

# Running Tests

Run all tests from the repository root:

```powershell
dotnet test
```

Run a specific test project:

```powershell
dotnet test EmployeeManager.Tests
```

or:

```powershell
dotnet test EmployeeManagerApi.IntegrationTests
```

---

# Azure Deployment

The production deployment consists of two primary Azure resources:

```text
┌─────────────────────────────┐
│       Azure App Service     │
│                             │
│  EmployeeManager.API        │
│       ASP.NET Core          │
└──────────────┬──────────────┘
               │
               │ Encrypted SQL connection
               ▼
┌─────────────────────────────┐
│      Azure SQL Database     │
│                             │
│       db_employee           │
└─────────────────────────────┘
```

## 1. Create an Azure SQL Database

In the Azure Portal:

1. Create an **Azure SQL logical server**.
2. Create an **Azure SQL Database**.
3. Name the database:

```text
db_employee
```

4. Configure the required firewall/network access.
5. Obtain the SQL connection information.

For production environments, configure network access according to your organization's security requirements.

---

## 2. Configure Azure SQL Firewall

The Azure App Service must be able to connect to Azure SQL.

Depending on the networking configuration, this can be achieved using the appropriate Azure SQL firewall/networking configuration.

For a basic deployment, configure the Azure SQL server networking settings to permit the App Service to connect.

For more secure production architectures, consider:

* Private Endpoints
* Virtual Networks
* VNet Integration
* Private DNS
* Restricted firewall rules
* Managed Identity / Microsoft Entra authentication

---

# 3. Create Azure App Service

Create an Azure App Service configured for:

```text
Runtime: .NET 10
Operating System: Linux or Windows
```

The exact runtime options available depend on the Azure App Service environment and current Azure support.

The App Service hosts:

```text
EmployeeManager.API
```

---

# 4. Configure the Production Connection String

In the Azure Portal:

```text
App Service
   → Settings
   → Environment variables
```

Add the database connection string using the name:

```text
EmployeeDB
```

For example:

```text
EmployeeDB = Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=db_employee;...
```

The application reads the connection string using:

```csharp
builder.Configuration.GetConnectionString("EmployeeDB");
```

This allows the same application code to work with different databases in different environments.

For example:

```text
Development
    ↓
LocalDB
    ↓
db_employee

Production
    ↓
Azure App Service
    ↓
Azure SQL Database
    ↓
db_employee
```

---

# 5. Deploy the Application

The application can be deployed to Azure App Service using several methods.

Common options include:

* Visual Studio
* VS Code
* Azure CLI
* GitHub Actions
* Azure DevOps

For CI/CD, GitHub Actions can be configured so that:

```text
GitHub Repository
       │
       ▼
GitHub Actions
       │
       ├── Restore
       ├── Build
       ├── Test
       ├── Publish
       │
       ▼
Azure App Service
```

A typical build sequence is:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet publish EmployeeManager.API --configuration Release
```

The published application is then deployed to the Azure App Service.

---

# Database Migrations in Azure

Before the production application uses the database, the Azure SQL Database must have the required schema.

EF Core migrations can be applied against the Azure SQL Database using the production connection string.

For example:

```powershell
dotnet ef database update `
  --project EmployeeManager.Infrastructure `
  --startup-project EmployeeManager.API `
  --configuration Release
```

Make sure the command is executed in an environment where the Azure SQL connection string is securely available.

For automated deployments, migrations can also be incorporated into a controlled CI/CD deployment process.

> **Recommendation:** Production database migrations should be treated as a deployment operation and tested before being applied to the production database.

---

# Environment Configuration

A recommended configuration is:

### Development

```text
appsettings.json
        │
        ▼
Local SQL Server / LocalDB
```

### Production

```text
Azure App Service Environment Variables
        │
        ▼
Azure SQL Database
```

Do not store production credentials directly in:

```text
appsettings.json
```

or:

```text
appsettings.Production.json
```

Instead, configure them through Azure App Service environment variables / connection strings.

---

# Viewing the Azure SQL Database

You can inspect the production database using:

* SQL Server Management Studio (SSMS)
* Azure Portal Query Editor, where available
* Azure Data Studio
* Visual Studio SQL Server Object Explorer

Use the Azure SQL server and database credentials/configuration supplied when the database was created.

After connecting, the database should contain tables such as:

```text
dbo.Employees
dbo.Departments
```

---

# API Documentation

When Swagger is enabled, the API documentation can be accessed through:

```text
/swagger
```

For a deployed App Service:

```text
https://<app-service-name>.azurewebsites.net/swagger
```

The exact URL depends on the deployed App Service name and application configuration.

---

# Project Configuration

The application expects the connection string to be named:

```text
EmployeeDB
```

Example application configuration:

```json
{
  "ConnectionStrings": {
    "EmployeeDB": "..."
  }
}
```

The actual production value should be supplied by Azure App Service configuration rather than committed to source control.

---

# Security Considerations

The following practices should be followed when deploying the application:

* Do not commit database passwords to Git.
* Do not commit production connection strings containing credentials.
* Use HTTPS for the production API.
* Restrict Azure SQL firewall access.
* Use strong database credentials where SQL authentication is used.
* Consider Microsoft Entra authentication and Managed Identity for production.
* Consider Azure Key Vault for sensitive configuration.
* Keep .NET and NuGet dependencies updated.
* Run automated tests before deployment.
* Restrict database permissions according to the application's requirements.

---

# Useful Commands

### Restore dependencies

```powershell
dotnet restore
```

### Build

```powershell
dotnet build
```

### Run

```powershell
dotnet run --project EmployeeManager.API
```

### Run tests

```powershell
dotnet test
```

### Create an EF Core migration

```powershell
dotnet ef migrations add <MigrationName> `
  --project EmployeeManager.Infrastructure `
  --startup-project EmployeeManager.API
```

### Apply migrations

```powershell
dotnet ef database update `
  --project EmployeeManager.Infrastructure `
  --startup-project EmployeeManager.API
```

### Publish

```powershell
dotnet publish EmployeeManager.API `
  --configuration Release
```

---

# Development Workflow

A typical development workflow is:

```text
1. Clone repository
       ↓
2. Restore .NET dependencies
       ↓
3. Configure local EmployeeDB connection
       ↓
4. Apply EF Core migrations
       ↓
5. Run EmployeeManager.API
       ↓
6. Test API using Swagger
       ↓
7. Run automated tests
       ↓
8. Commit changes
       ↓
9. CI/CD build and test
       ↓
10. Deploy to Azure App Service
       ↓
11. Connect to Azure SQL Database
```

---

# Production Architecture

The intended production environment is:

```text
                         INTERNET
                            │
                            │ HTTPS
                            ▼
                 ┌─────────────────────┐
                 │   Azure App Service │
                 │                     │
                 │ EmployeeManager.API │
                 │     ASP.NET Core    │
                 └──────────┬──────────┘
                            │
                            │ SQL/TLS
                            ▼
                 ┌─────────────────────┐
                 │     Azure SQL       │
                 │      Database       │
                 │                     │
                 │    db_employee      │
                 └─────────────────────┘
```

This separates the application tier from the database tier while allowing the same ASP.NET Core application to use LocalDB during development and Azure SQL Database in production.

---

# License

This project is for educational and development purposes unless otherwise specified.
