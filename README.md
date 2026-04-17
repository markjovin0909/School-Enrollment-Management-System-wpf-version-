# School Enrollment Management System (WPF)

A Windows desktop school management and enrollment application built with WPF, .NET 8, Entity Framework Core, and MySQL.

This repository contains the full solution for a school operations system that covers student and teacher management, enrollment processing, scheduling, curriculum setup, compliance workflows, backup and restore, and administrative governance tooling.

## Overview

The application starts with a login flow and loads into a multi-module operations workspace. The main shell is organized around these areas:

- Dashboard
- Students
- Teachers
- Enrollment
- Reports
- Master Data
- Scheduling
- Accounts and Compliance
- Maintenance

The codebase uses a service and repository structure over an EF Core `AppDbContext`, with WPF windows for each operational module.

## Core Features

### Student and teacher administration

- Student records with profile data, status, and preferred curriculum
- Teacher records and account administration
- Password change and account recovery flows
- Activity history and audit visibility for operational actions

### Enrollment workflow

- Enrollment creation and updates tied to school year, section, and curriculum
- Enrollment state handling for `PENDING`, `ENROLLED`, and `RESERVED`
- Waitlist and queue SLA tracking
- Requirement-aware enrollment validation
- Year-end rollover support

### Academic setup and scheduling

- School settings and school year management
- Grade levels, subjects, curriculum, and sections
- Class offerings and schedules
- Rooms and time slots
- Teacher load management

### Compliance and governance

- Student requirements tracking
- Archive center
- Audit and governed operation logging
- Exception queue support
- Governance documents, risk matrices, flow packs, and backlog artifacts
- Governance smoke-check utility under `Tools/GovernanceSmoke`

### Reporting and maintenance

- Dashboard metrics and warnings
- CSV and PDF export services
- Backup and restore workflows
- Operational metrics reporting
- Included Visual Studio setup project under `SMS Setup`

## Tech Stack

- C#
- WPF
- .NET 8 (`net8.0-windows`)
- Entity Framework Core 8
- MySQL via `MySql.EntityFrameworkCore`
- Visual Studio setup project (`.vdproj`) for installer packaging

## Solution Layout

```text
.
|-- School Management System(wpf).sln
|-- README.md
|-- School Management System(wpf)/
|   |-- App.xaml / App.xaml.cs
|   |-- MainWindow*.cs
|   |-- Views/
|   |-- Controls/
|   |-- Models/
|   |-- Services/
|   |-- Repositories/
|   |-- Interfaces/
|   |-- Data/
|   |   |-- AppDbContext.cs
|   |   |-- EfMigrations/
|   |   |-- Migrations/
|   |   `-- Seeds/
|   |-- Configuration/
|   |-- Governance/
|   `-- Tools/
`-- SMS Setup/
```

## Prerequisites

Before running the application, make sure you have:

- Windows 10 or 11
- Visual Studio 2022 with .NET desktop development workload
- .NET 8 SDK
- MySQL Server 8.x or a compatible MySQL instance

## Getting Started

### 1. Clone the repository

```powershell
git clone https://github.com/markjovin0909/School-Enrollment-Management-System-wpf-version-.git
cd "School-Enrollment-Management-System-wpf-version-"
```

### 2. Open the solution

Open `School Management System(wpf).sln` in Visual Studio.

### 3. Configure the database connection

Database settings live in:

- `School Management System(wpf)/App.config`
- `School Management System(wpf)/Configuration/README.md`

This repository stores placeholder values only. Update the connection string for the environment you want to use:

```xml
<add key="ActiveEnvironment" value="Local" />

<add name="DbLocal"
     connectionString="server=localhost;uid=root;password=YOUR_PASSWORD;database=School_sms"
     providerName="MySql.Data.MySqlClient" />
```

Available environments:

- `Local`
- `Remote`
- `Online`

### 4. Create or upgrade the database

Migration scripts are located in `School Management System(wpf)/Data/Migrations`.

Recommended run order for a fresh database:

1. `20260307_ef_workflow_aligned_schema.sql`
2. `20260307_admin_feature_extensions.sql`
3. `20260312_gap_closure_features.sql`
4. `20260409_structural_governance_framework.sql`
5. `../Seeds/20260227_seed_school_sms.sql`

The migration notes are documented in:

- `School Management System(wpf)/Data/Migrations/README.md`

### 5. Build and run

From Visual Studio, set the WPF project as the startup project and run it.

Or from the command line:

```powershell
dotnet build "School Management System(wpf).sln"
dotnet run --project ".\School Management System(wpf)\School Management System(wpf).csproj"
```

## Main Modules

### Dashboard

- Student, teacher, section, and offering counts
- Enrollment status chart
- Requirement, waitlist, and backup health warnings
- Operational metrics snapshot

### Students

- Student registration and updates
- Preferred curriculum assignment
- Requirements view
- Student account activity access

### Teachers

- Teacher record management
- Teacher credential and activity support

### Enrollment

- Enrollment draft creation
- Validation and approval-related workflows
- Queue visibility and reserved-slot handling

### Reports

- Export-oriented reporting backed by CSV and PDF services
- Preset history support

### Operations Workspace

The operations section hosts these modules inline:

- School Settings
- School Years
- Grade Levels
- Subjects
- Curriculum
- Sections
- Class Offerings
- Schedules
- Teacher Loads
- Rooms
- Time Slots
- Student Accounts
- Student Requirements
- Archive Center
- Backup and Restore
- Year-End Rollover

## Architecture Notes

- `Views/` contains WPF windows and UI modules.
- `Services/` contains business logic and orchestration.
- `Repositories/` provides data access abstractions and implementations.
- `Models/` defines entities and enums.
- `Data/AppDbContext.cs` defines the EF Core data model.
- `Governance/` stores process documentation, rollout assets, and signoff materials.
- `Tools/GovernanceSmoke/` contains a smoke-check utility for governance-related validation.

## Security Notes

- Do not commit real database credentials.
- Keep production connection strings outside source control where possible.
- Review `Configuration/README.md` for environment handling guidance.
- Use different credentials for local, staging, and production databases.

## Packaging

The repository includes a Visual Studio setup project:

- `SMS Setup/SMS Setup.vdproj`

You can use it to produce installer artifacts for Windows deployment.

## Status

This is an actively structured desktop application codebase with operational, governance, and deployment assets included in the repository.

## License

No license file is currently included. Add one if you want to define reuse terms explicitly.
