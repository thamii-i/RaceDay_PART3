# RaceDay

RaceDay is a full-stack event management system for the South African road running, walking and cycling community.
Organisers create and manage events, categories, enrolments and results; Participants browse events, enrol, and view their
own published results.

| Part | Content |
|------|---------|
| 1 | ERD, API endpoint plan and SQL script (`docs/`) |
| 2 | ASP.NET Core Web API, EF Core code-first, Swagger, unit tests, GitHub Actions (`src/RaceDay.Api`, `tests/`) |
| 3 | ASP.NET Core MVC front end that consumes the API, with image uploads saved in the API's `wwwroot` (`src/RaceDay.Web`) |

Student: _your name / student number_ | Repository: _your GitHub URL_

## Repository structure

```
RaceDay/
├── RaceDay.sln                         Open in Visual Studio 2022
├── docs/
│   ├── RaceDay_ERD.png / .pdf          Entity Relationship Diagram
│   ├── RaceDay_API_Endpoint_Plan.docx  API endpoint plan (also as .md)
│   └── RaceDay_Database.sql            SQL Server script (schema + seed data)
├── src/RaceDay.Api/                    Web API (wwwroot/uploads holds the uploaded images)
├── src/RaceDay.Web/                    MVC application
├── tests/RaceDay.Tests/                xUnit tests for the API
└── .github/workflows/ci.yml            Build and test on every push / pull request
```

## Step by step: run it and store data in SSMS

Requirements: Visual Studio 2022 (17.8 or later) with the **ASP.NET and web development** workload, the .NET 8 SDK and SQL Server
Management Studio (SSMS) connected to a SQL Server instance.

1. **Create the database in SSMS.** Connect in SSMS, choose *File > Open > File*, open `docs/RaceDay_Database.sql` and click
   **Execute**. Refresh *Databases*: `RaceDay` now exists with seven tables and sample data.
2. **Point the API at the same server.** In SSMS note the **Server name** you connect with (shown in the Connect dialog, or
   right-click the server > *Properties*). Open `src/RaceDay.Api/appsettings.json` and set `Server=` to exactly that name:

   | SSMS server name | Connection string |
   |------------------|-------------------|
   | `(localdb)\MSSQLLocalDB` | `Server=(localdb)\\MSSQLLocalDB;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
   | `.\SQLEXPRESS` | `Server=.\\SQLEXPRESS;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
   | `localhost` or `.` | `Server=localhost;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
   | `PCNAME\SQLEXPRESS` | `Server=PCNAME\\SQLEXPRESS;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |

   In the JSON file every backslash is written twice (`\\`). If you log in to SSMS with a SQL login instead of Windows
   authentication, replace `Trusted_Connection=True` with `User Id=yourlogin;Password=yourpassword`.
3. **Start both projects.** Right-click the solution > *Configure Startup Projects* > *Multiple startup projects*, set
   `RaceDay.Api` and `RaceDay.Web` to **Start**, then press **F5**.
   * API: http://localhost:5180 (Swagger at `/swagger`)
   * MVC website: http://localhost:5190
4. **Confirm the connection.** In the API console window (or Visual Studio's *Output* window) look for
   `Connected to SQL Server '...', database 'RaceDay'`. That line shows exactly where your data is saved.
5. **Test it.** Register a user or create an event on the website (or in Swagger), then in SSMS run
   `SELECT * FROM Participants;` or `SELECT * FROM Events;` (right-click *RaceDay* > *New Query*). The new rows are there.
6. **Seeded accounts** (password for all: `Password123!`): organisers `thabo@gmail.com`, `sarah@gmail.com`; participants
   `liam@gmail.com`, `amahle@gmail.com`, `jason@gmail.com`, `naledi@gmail.com`.

If the API does not start or the log says it could not connect, the most common causes are a wrong `Server=` name, the script
from step 1 not having been executed, or SQL Server not running (open *SQL Server Configuration Manager* or *Services* and
start the SQL Server service).

To use EF Core migrations instead of the script, open *Tools > NuGet Package Manager > Package Manager Console*, set
**Default project** to `RaceDay.Api` and run `Add-Migration InitialCreate` then `Update-Database`.

## The MVC application

* **No database access and no business logic:** `RaceDay.Web` talks only to the API through a typed `HttpClient`
  (`Services/RaceDayApi.cs`); its address is `ApiSettings:BaseUrl` in `src/RaceDay.Web/appsettings.json`. Validation,
  ownership rules, bib numbers and result publishing live in the API.
* **Authentication and session management:** users register and log in through the API. The returned JWT, user id and role
  are kept in a secure, HttpOnly cookie session that expires with the token. Protected pages require login (otherwise the
  user is redirected to the login page) and each role has its own area. Log out ends the session and returns to the home page.
* **Organiser interface:** dashboard (events, total enrolments per event, upcoming dates), event management with banner upload,
  category management, enrolments page with status control, results entry with publish/unpublish, organisation profile.
* **Participant interface:** upcoming events with search and type filters, event detail page with banner and categories,
  one-step enrolment with category selection, my enrolments with status, my results (event, date, category, finish time and
  position such as 47th out of 312 finishers), and a profile page with profile picture upload.
* **Enrolment statuses:** Pending (amber), Confirmed (green) and Cancelled (red). New enrolments start as Pending and the
  Organiser confirms or cancels them.
* **Images (wwwroot):** the MVC site uploads images to the API (`POST /api/events/{id}/banner`,
  `POST /api/users/me/picture`). The API validates them (JPEG, PNG, WebP or GIF, up to 5 MB) and saves them in
  `src/RaceDay.Api/wwwroot/uploads/banners` and `.../profiles`; only the relative path is stored in the database
  (`Events.BannerImage`, `Participants.ProfilePicture`). Images are loaded through `GET /api/images/{path}`, which the MVC site
  proxies at `/media/{path}`.
* **Error handling:** invalid input is validated in the browser and on the server; API failures are turned into friendly
  messages and never shown as raw errors.
* **UI:** Bootstrap 5 with a custom sporting theme (navy, orange and lime, Barlow Condensed headings), consistent components
  and a role-specific navigation menu. Bootstrap, Bootstrap Icons, jQuery validation and Google Fonts load from CDNs.

## Database

Built on the supplied `Raceday.sql` (Organisers, Participants, Events, Categories, EventCategories, Enrollments). Additions
required by the brief, reflected in the ERD, SQL script, EF Core model and endpoint plan:

* `PasswordHash` on Organisers and Participants.
* `Description`, `EventType`, `BannerImage` and `ResultsPublished` on Events.
* `ProfilePicture` on Participants.
* `CategoryID` on Enrollments, and a status of Pending, Confirmed or Cancelled (default Pending).
* A `Results` table (finish time and finishing position, one per enrollment).

The EF Core model (`Data/RaceDayDbContext.cs`) uses the same table names, column types, keys, unique constraints, check
constraints and defaults as the script.

## API and tests

* 29 endpoints are planned in `docs/RaceDay_API_Endpoint_Plan.docx` and listed in Swagger at `/swagger`. In Swagger, log in
  through `POST /api/auth/login`, click **Authorize** and paste the token.
* Role management: a user chooses Organiser or Participant at registration; any other role is rejected.
* Passwords are hashed with ASP.NET Core Identity's PBKDF2 `PasswordHasher`.
* Run the tests with *Test > Run All Tests* or `dotnet test RaceDay.sln`. They use an in-memory database and an in-memory
  image store, so no SQL Server is needed. They cover registration and login, event management, role-based access,
  enrolments and statuses, results and publication, and image uploads, including failure scenarios.
* GitHub Actions (`.github/workflows/ci.yml`) restores, builds and tests the solution.
* `Jwt:Key` in `appsettings.json` is a development value; replace it for any real deployment.
