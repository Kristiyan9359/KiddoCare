# KiddoCare

KiddoCare is a kindergarten management web application built with ASP.NET Core MVC and .NET 10. The project focuses on day-to-day communication between administrators, teachers, and parents, with role-based access to children, attendance, reports, documents, events, announcements, and consent workflows.

## Project Status

The project is currently under active development. Core functionality, role-based access rules, file access protections, and service-level tests are already implemented. UI polish, demo data, screenshots, and deployment preparation are planned next.

## Features

### Administrator

- Manage kindergarten groups
- Manage child profiles and local child photo uploads
- Manage parent and teacher profiles
- Create and manage events and announcements
- Track attendance and attendance history
- Create and manage medical records
- Review uploaded child documents
- View global dashboard statistics

### Teacher

- View children only from the assigned group
- Manage attendance for the assigned group
- Create daily reports for children in the assigned group
- Create group events and announcements
- View relevant absence notices, consent requests, documents, and activity feed items
- View a teacher-focused dashboard

### Parent

- View own children only
- View child details, medical summary, activity feed, daily reports, events, and announcements
- Submit absence notices
- Respond to consent requests
- Upload child documents
- View a parent-focused dashboard

## Security And Access Control

- ASP.NET Core Identity authentication
- Role-based authorization for Admin, Teacher, and Parent users
- Teacher access limited to assigned group
- Parent access limited to own children
- Global antiforgery token validation for MVC forms
- Local file download protections for child documents and child photos
- File path containment checks to prevent path traversal
- Seeded user passwords loaded from configuration/user secrets instead of hardcoded values

## Testing

The solution includes unit and integration tests for the most important business and security rules.

Current test status:

```text
139 passed, 0 failed
```

Covered areas:

- Service-level business rules
- Role-based data access
- Dashboard filtering
- Attendance and daily report rules
- Absence notices and consent requests
- Child document workflow
- Medical record rules
- MVC authorization integration tests
- File access and path traversal integration tests
- View model validation tests

## Tech Stack

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- xUnit
- EF Core InMemory provider for tests
- Bootstrap

## Solution Structure

```text
KiddoCare.Common          Shared constants and validation attributes
KiddoCare.Data            EF Core DbContext, migrations, seed logic
KiddoCare.Data.Models     Entity models and enums
KiddoCare.Services.Core   Business logic services
KiddoCare.ViewModels      MVC view models
KiddoCare.Web             ASP.NET Core MVC web application
KiddoCare.Tests           Unit and integration tests
```

## Local Setup

1. Clone the repository.

2. Configure user secrets for the web project.

```powershell
cd KiddoCare.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-sql-server-connection-string"
dotnet user-secrets set "SeedUsers:AdminPassword" "your-admin-password"
dotnet user-secrets set "SeedUsers:DefaultParentPassword" "your-default-parent-password"
dotnet user-secrets set "SeedUsers:DefaultTeacherPassword" "your-default-teacher-password"
```

3. Apply database migrations.

```powershell
dotnet ef database update --project ../KiddoCare.Data --startup-project .
```

4. Run the application.

```powershell
dotnet run
```

## Test Commands

Run all tests:

```powershell
dotnet test
```

Run the test project directly:

```powershell
dotnet test ../KiddoCare.Tests/KiddoCare.Tests.csproj
```

## Demo Accounts

Demo accounts will be finalized with the demo seed data before deployment.

Planned demo roles:

- Admin
- Teacher
- Parent

## Roadmap

- Improve overall UI and responsive layout
- Add polished dashboards and better visual hierarchy
- Add production-ready demo seed data
- Add screenshots to this README
- Prepare deployment configuration
- Add localization support
- Replace temporary default-password workflow with a proper invitation or password reset flow

