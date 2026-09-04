# Server Room Monitor

Server Room Monitor is an ASP.NET Core web application for managing server
rooms, inspections, technicians, schedules, reminders, reports, and
predictive maintenance.

## Features

- Server-room management
- Technician accounts and role-based access
- Inspection scheduling and calendar views
- Temperature and equipment condition checks
- Inspection attempts, notes, and history
- Inspection reminders and email notifications
- Admin dashboard and statistics
- Inspection and report PDF generation
- Predictive-maintenance data and failure prediction
- ML.NET model training, tuning, and testing

## Technology

- .NET 10
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- ML.NET with LightGBM and FastTree
- QuestPDF
- MailKit
- Bootstrap and jQuery

## Requirements

- .NET 10 SDK
- SQL Server or SQL Server LocalDB
- SMTP account for email notifications

## Installation

Clone the repository and open the project directory:

```bash
git clone https://github.com/azizchaabi/Server-Room.git
cd Server-Room
```

Restore the project dependencies:

```bash
dotnet restore
```

## Configuration

Configure a SQL Server connection string named `DefaultConnection`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ServerRoomMonitor;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Email notifications use the following settings:

```json
{
  "EmailSettings": {
    "Host": "smtp.example.com",
    "Port": "587",
    "Username": "smtp-user",
    "Password": "smtp-password",
    "FromEmail": "server-room-monitor@example.com"
  },
  "ApplicationUrl": "https://localhost:7171"
}
```

For local development, use .NET User Secrets to keep passwords and credentials
outside the project files:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "EmailSettings:Host" "smtp.example.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:Username" "smtp-user"
dotnet user-secrets set "EmailSettings:Password" "smtp-password"
dotnet user-secrets set "EmailSettings:FromEmail" "server-room-monitor@example.com"
dotnet user-secrets set "ApplicationUrl" "https://localhost:7171"
```

## Database

Apply the Entity Framework Core migrations:

```bash
dotnet ef database update
```

Create a migration after changing the data model:

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## Running the application

Start the application with:

```bash
dotnet run
```

The development URL is:

- http://localhost:5263

## User roles

### Admin

Administrators can:

- Manage server rooms
- Create and manage users
- Assign technicians
- Schedule inspections
- View calendars, dashboards, and statistics
- Review reports and reminders
- Manage predictive-maintenance tools

### Technician

Technicians can:

- View assigned inspections
- Complete scheduled inspections
- Record inspection checks and notes
- Review reminders
- View inspection history

The application creates the `Admin` and `Technician` roles automatically at
startup. New accounts are created by an administrator.

## Predictive maintenance

The predictive-maintenance module uses ML.NET to estimate whether a server-room
failure may occur within seven days.

It uses information such as:

- Temperature and temperature deviation
- Recent failed inspections and attempts
- Previous room problems
- Overdue inspections
- Time since the last repair
- Air conditioning, power, water-leak, alarm, and cleanliness checks

Administrators can generate sample data, train the model, tune model
parameters, view feature importance, and test predictions from the Admin tools.

## Project structure

```text
Areas/Identity/       Identity login, logout, and registration pages
Data/                 Entity Framework Core database context
ML/                   Predictive-maintenance model services
Migrations/           Entity Framework Core migrations
Models/               Application data models
Pages/                Razor Pages and application workflows
Services/             Email, reminders, PDF, and data services
wwwroot/              CSS, JavaScript, and client libraries
Program.cs            Application configuration and startup
```