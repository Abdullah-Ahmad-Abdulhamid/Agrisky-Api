🌾 AgriSky API
AgriSky is a robust E-commerce and Management API built with ASP.NET Core 8. It serves as the backend engine for the AgriSky platform, connecting farmers, suppliers, and customers within a digital agricultural marketplace.

🚀 Features
User Management: Secure Authentication & Authorization using JWT (JSON Web Tokens).

Product Catalog: Full CRUD operations for agricultural products and categories.

Shopping Cart: Persistent cart management for users.

Order System: Complete workflow from order placement to shipping tracking.

Seed Data: Automatic database seeding for Admin accounts and default categories.

Security: Password hashing using BCrypt and secure CORS policy configuration.

Social Auth: Integrated with Google Client ID for future social login support.

🛠️ Tech Stack
Framework: .NET 8.0 (ASP.NET Core Web API)

Database: Microsoft SQL Server (LocalDB for development)

ORM: Entity Framework Core

Security: JWT, BCrypt.Net

Documentation: Swagger / OpenAPI

⚙️ Getting Started
1. Prerequisites
.NET 8.0 SDK

SQL Server Express or LocalDB

2. Configuration
The project uses appsettings.json for configuration. For production, sensitive data should be managed via Environment Variables or Secrets Manager.

JSON
{
  "ConnectionStrings": {
    "conn": "Your_SQL_Server_Connection_String"
  },
  "Jwt": {
    "Key": "Your_Secret_Key_Min_32_Chars",
    "Issuer": "AgriskyApp",
    "Audience": "AgriskyUsers"
  }
}
3. Installation
Clone the repository:

Bash
git clone https://github.com/Abdullah-Ahmad-Abdulhamid/Agrisky-Api.git
cd AgriskyApi
Restore dependencies:

Bash
dotnet restore
Update Database:
The project uses EF Core Migrations. Run the following to create your local database:

Bash
dotnet ef database update
Run the application:

Bash
dotnet run
🔑 Default Credentials (Seed Data)
Upon first run, the database is seeded with an administrative account:

Email: omar.0523025@gmail.com

Password: Admin@AgriSky2024 (Please change this after first login)

📁 Project Structure
/Controllers: API Endpoints (Auth, Products, Orders, etc.)

/Models: Entity definitions and Database Context (AppDbcontext)

/DTOs: Data Transfer Objects for clean API request/response handling

/Data: Seed Data logic and Migrations

🛡️ Security Note
Never commit real production secrets (API Keys, DB Passwords) to the repository. This project is configured to use .gitignore to protect environment-specific settings.

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
