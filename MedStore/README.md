# MediStock · Medical Store Management System

An ASP.NET Core MVC classroom application for the Semester 5 .NET Technologies practical. It runs without SQL Server and saves local records to an ignored JSON file under `App_Data`.

## Technology

- C# / ASP.NET Core MVC on .NET 10
- Local JSON-file storage (no database required)
- Cookie authentication, role authorization, anti-forgery tokens, DataAnnotations validation
- Responsive custom UI; no client side framework required

## Run locally

1. Open `MedStore.csproj` in Visual Studio 2026, or use the .NET 10 SDK.
2. Run `dotnet run` and open the HTTPS URL printed by the app.
3. Initial demo login: **admin** / **Admin@123**. Change this password in User management before sharing the application. This file-backed demo is intended for a single local instance, not public deployment.

## Included flows

- Login first, local demo credentials, Admin role claims, persistent secure cookie option and logout/session clearing.
- Dashboard counts, inventory valuation, seven-day sales chart, recent sales, low-stock and expiry alerts.
- Medicine CRUD, category/manufacturer/expiry filters, sorting, pagination, batch validation, and CSV export.
- Category, supplier, customer, employee, expense, feedback, leave, user, and store settings management.
- Purchases add stock; sales apply discounts, prevent expired/over-stock sales, save invoices, and reduce stock. Sale cancellation reverses the stock movement.
- Stock adjustments and movement history; monthly payroll generation and printable salary slips; leave approval/rejection; printable sales and purchase invoices; reports.
- `GET /api/medicines?search=...` returns authenticated inventory JSON for permitted roles.
- Passwords use ASP.NET Core's salted password hasher. Login uses local user records and does not connect to SQL Server.
- Records persist between restarts in `App_Data/medistock-demo.json`. Back up this file to preserve the demo records.

## Current scope

The pasted prompt ends at “# 15.”, so requirements after that heading were not available. The app is a single-instance local educational demo using JSON files rather than a multi-user production database. It does not implement external email-based password reset, barcode hardware, or deployment configuration.
