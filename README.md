## PART 1 - System Description

RaceDay is a full-stack, web-based event management system built for the South African road running, walking, and cycling community.
It replaces the paper-based registration, spreadsheets, and disconnected communication that many local road events
(park runs, charity walks, community cycling events) currently rely on.

The platform allows Event Organisers to create and manage events, define categories (for example, distance-based race categories), and capture participant results.
Participants can browse upcoming events, enter events by enrolling in a category, track their personal performance history across past events,
and prepare for race day using live weather and route information.

This repository covers Part 1:
System Planning and Database — the ERD, API endpoint plan, and SQL database script that were completed before any application code was written.

Roles:
Organiser — creates and manages Events and their Categories, views who has enrolled in their events, and captures results once a race is completed.
An organiser can only manage the events they created themselves.
Participant — browses public events and categories, enrols in a category, views their own enrolment history,
and can view their own results and overall performance history across multiple events.

Database Design (ERD)

See docs/raceday_erd.docx, the data model consists of six entities:
Organiser, Participant, Event, Category, Enrolment, and Result.
Enrolment is the associative table resolving the many-to-many relationship between Participants and Categories;
Result stores one outcome per Enrolment.

API Endpoint Plan

See docs/api_endpoint_plan.md.docx for the full table of planned endpoints
(Method, Route, Description, Role Required, Request Body, Expected Response),
covering authentication, user profile, events, categories, enrolments, and results.

SQL Script

See docs/raceday_database.sql. Run in SQL Server Management Studio (SSMS).
The script creates all six tables with primary keys, foreign keys, and constraints,
and seeds the database with 2 Organisers, 2 Participants, 3 Events, categories for each event, and sample enrolments/results.

CI/CD

A GitHub Actions workflow (.github/workflows/validate-docs.yml) runs on every push and pull request to main,
validating that the /docs folder exists and contains the ERD, API endpoint plan, and SQL script.

# Part 2 - RaceDay RESTful API

A RESTful API for managing race events, participants, event enrolments, and user authentication. The API uses role-based access control to help ensure that users can only access functionality permitted by their assigned roles.

Features:
User Authentication: User registration and login using JWT (JSON Web Token) authentication.
Role-Based Access Control: Restricts protected endpoints according to user roles.
Event Management: Allows authorised Organisers to manage race events.
Event Enrolments: Allows Participants to enrol in events.
Categories: Provides functionality for managing event categories.
Participant Management: Supports participant-related operations.
Profile Management: Provides profile-related functionality.
Automated Testing: Includes integration tests for authentication, event management, enrolments, and role-based access.

Technologies Used:
C#
ASP.NET Core Web API (.NET 10)
Entity Framework Core
Microsoft SQL Server
JWT Authentication
xUnit
ASP.NET Core MVC Testing

Project Structure:

RaceDay.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── CategoriesController.cs
│   ├── EnrolmentController.cs
│   ├── EventController.cs
│   └── ParticipantController.cs
├── Data/
│   └── AppDbContext.cs
├── Dtos/
├── Migrations/
├── Models/
├── Services/
│   ├── JwtService.cs
│   └── PasswordService.cs
├── Program.cs
└── appsettings.json

RaceDay.API.Tests/
├── AuthTests.cs
├── CategoryTests.cs
├── EnrolmentTests.cs
├── EventTests.cs
├── ProfileTests.cs
├── ResultTests.cs
├── RoleAccessTests.cs
├── RuleTests.cs
└── TestSupport.cs

The two roles:
Organiser - creates, updates and deletes *their own* events; defines categories for them (name, distance, max
participants, entry fee); views all enrolments for their events; captures finish time, position and status for
enrolments in their events. Cannot enrol in events.

Participant - browses events and categories; enrols in a category (one enrolment per category, blocked when
the category is full); views and cancels their own enrolments; views their own results and full performance
history. Cannot create or change events, categories or results.

Anyone can register as either role (the role is chosen at registration). Browsing events/categories is public;
everything else requires login, and role/ownership is checked on every protected endpoint
(401 = not logged in, 403 = wrong role or not the owner).

Prerequisites:
Before running the project, ensure that you have installed:
Visual Studio 2022 or a compatible IDE that supports .NET 10.
The .NET 10 SDK.
Microsoft SQL Server.
SQL Server Management Studio (SSMS) or another suitable database management tool.
Git.

Installation:
Clone the repository:
git clone <YOUR_GITHUB_REPOSITORY_URL>
Navigate to the solution directory:
cd RaceDay.API
Open the solution file in Visual Studio.
Restore the project's NuGet packages.

Setup and run:
1. Install Visual Studio with the ASP.NET workload, .NET SDK matching the project, and SQL Server (LocalDB is fine).
2. Clone the repo and open `RaceDay_API.slnx` (or the solution in the Part 2 folder).
3. Check `ConnectionStrings:DefaultConnection` in `RaceDay.API/appsettings.json` points at your SQL Server.
4. Create the database from the models. In **Package Manager Console** (Default project = `RaceDay.API`):
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
5. Run `RaceDay.API` (https profile). Swagger opens at `https://localhost:<port>/swagger`.
6. In Swagger, register a test account with the Organiser role and another with the Participant role. Log in with each account using `POST /api/auth/login`, then authorise requests with the JWT returned by the API (use the Authorize control or the `Authorization: Bearer <token>` header, as supported by the Swagger configuration). Test role-restricted endpoints with both roles. Use `POST /api/auth/logout` where available, or log out by ending the authenticated session/token according to the API implementation before switching accounts.

Endpoints (from the Part 1 plan)
| # | Method | Route | Access |
|---|---|---|---|
| 1 | POST | /api/auth/register | Public |
| 2 | POST | /api/auth/login | Public |
| 3 | GET | /api/profile | Logged in |
| 4 | PUT | /api/profile | Logged in |
| 5 | POST | /api/events | Organiser |
| 6 | GET | /api/events | Public |
| 7 | GET | /api/events/{id} | Public |
| 8 | PUT | /api/events/{id} | Organiser (owner) |
| 9 | DELETE | /api/events/{id} | Organiser (owner) |
| 10 | POST | /api/events/{id}/categories | Organiser (owner) |
| 11 | GET | /api/events/{id}/categories | Public |
| 12 | PUT | /api/categories/{id} | Organiser (owner) |
| 13 | DELETE | /api/categories/{id} | Organiser (owner) |
| 14 | POST | /api/categories/{id}/enrolments | Participant |
| 15 | GET | /api/enrolments/customer | Participant |
| 16 | GET | /api/events/{id}/enrolments | Organiser (owner) |
| 17 | DELETE | /api/enrolments/{id} | Participant (owner) |
| 18 | POST | /api/enrolments/{id}/result | Organiser (owner) |
| 19 | GET | /api/enrolments/{id}/result | Logged in (own result / own event) |
| 20 | GET | /api/participants/me/history | Participant |

Extra: `POST /api/auth/logout` ends the session.

Database Configuration:
Configure the database connection string in appsettings.json or through your development environment's configuration settings.
Example connection string:
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SQL_SERVER_CONNECTION_STRING"
  }
}
Replace the placeholder with your actual SQL Server connection string. Keep private credentials and production connection details out of source control.

JWT Configuration:
Configure the JWT settings expected by the application in appsettings.json or your development environment's configuration.
Use a secure signing key of at least 32 characters, as required by the application. Do not commit real signing keys or production secrets to GitHub.

Run the API:
In Visual Studio, set RaceDay.API as the startup project and run the application.
Alternatively, from the directory containing RaceDay.API.csproj, execute:
dotnet restore
dotnet run
The application will display the local address where the API is running.

RaceDay API integration tests:
These tests exercise the API through HTTP using `WebApplicationFactory<Program>`. They cover:
- Registration and login, including invalid credentials.
- Anonymous requests to protected endpoints.
- Organiser-only event creation and event ownership checks.
- Participant-only enrolment and checking that the enrolment is returned by the participant's enrolment endpoint.
- Wrong-role rejection and duplicate enrolment.
The test host replaces SQL Server with a uniquely named EF Core InMemory database, so tests do not require a local SQL Server instance.
Run locally
From the solution folder:
```bash
dotnet restore RaceDay.API.slnx
dotnet test RaceDay.API.slnx --configuration Release
```
The test project targets .NET 10 to match the API. Run the command above on a machine with the .NET 10 SDK installed. The test results recorded for this project are 5 passed, 0 failed, and 0 skipped; rerun the suite after making changes and confirm the GitHub Actions result.
Authentication and Authorisation
Protected endpoints require a valid JWT access token where authentication is enforced.

Include the token in the HTTP request's Authorization header:
Authorization: Bearer YOUR_ACCESS_TOKEN
Access to role-restricted functionality depends on the authenticated user's assigned role and the API's authorisation rules.

Continuous Integration:
The project is intended to run automated tests locally and through **GitHub Actions CI/CD**.
The GitHub Actions workflow should restore dependencies, build the solution, and run the test suite. Successful local test results do not by themselves confirm that the GitHub Actions workflow is configured or passing.

Screenshot of CI/CD workflow
<img width="1510" height="515" alt="image" src="https://github.com/user-attachments/assets/54940fa0-698e-4485-8986-9f37dd5a095a" />

YouTube Video link:
https://youtu.be/05gTS_h3Tv8

# Part 3 - RaceDay Web Application (ASP.NET Core MVC, Azure Blob Storage, Docker)

The RaceDay MVC web application is the front end for the Part 2 API. Organisers and Participants use it to create, browse,
enter and track road running, walking and cycling events. The site has **no database code and no business logic of its own**:
every piece of data comes from the Part 2 API, and every image upload goes through the API into Azure Blob Storage.

unlisted YouTube Link:

> Keep the Part 2 video link above as evidence for the API assessment. The Part 3 video should demonstrate the complete MVC application running through Docker.

Features:

| Area | What it does |
|---|---|
| Registration and login | Register as an Organiser or a Participant, log in through the API, log out. |
| Session management | After login the session stores the JWT, user ID, role and name. Logging out clears the session and revokes the token on the API. Protected pages redirect unauthenticated users to the login page. |
| Role-based navigation | Organisers and Participants see different menus. Visiting a page for the other role shows an "access denied" page. |
| Organiser | Dashboard (events, enrolments per event, upcoming dates), create/edit/delete events with a banner image, add/edit/remove categories, view enrolments, enter and correct results. |
| Participant | Home page with search and filters, event details with "Enter event", My Events (cancel an entry), My Results (finish time and position such as "47th of 312"), Profile with picture upload. |
| Azure Blob Storage | Event banners and profile pictures are uploaded from the MVC site to the API, stored in private Azure containers by the API, saved in the database as a URL, and shown on the pages. |
| Docker | The whole system (SQL Server, API and MVC site) starts with one command (see below). |
| UI | Bootstrap 5 with a RaceDay sporting theme. Enrolment and result statuses are colour coded (amber = registered, green = completed, red = did not finish or similar). |

How the pieces fit together

```
Browser  ->  RaceDay.MVC (ASP.NET Core MVC, session)  ->  RaceDay.API (REST, JWT, EF Core)  ->  SQL Server
                                                                    |
                                                                    +->  Azure Blob Storage (event banners, profile pictures)
```

* `RaceDay.MVC/Controllers` - one controller per area (Account, Home, Events, Participant, Organiser, Media).
* `RaceDay.MVC/Services/ApiClient.cs` - the only class that calls the API. It adds the logged-in user's token and turns every failure into a friendly message.
* `RaceDay.MVC/Infrastructure` - session helpers and the `[SessionAuthorize("Organiser")]` filter that protects pages.
* `RaceDay.MVC/Views` - Razor views, with shared layout, alerts, event card and status badge.
* `RaceDay.MVC/Models` - view models and simple copies of the API's responses.

Run the complete application with Docker Compose

Requirements

- Docker Desktop with Docker Compose.
- The repository's `.env` file, containing the required private configuration values.
- A prepared `RaceDayDb` database in the SQL Server Docker volume. For a fresh installation, create/restore the database and apply the Part 2 database setup before using the application. Do not assume an empty SQL Server volume already contains the RaceDay tables.

Configure private settings

Create a `.env` file in the repository root. Do not commit this file. It must contain the variables referenced by `docker-compose.yml`:

```dotenv
RACEDAY_DB_PASSWORD=REPLACE_WITH_YOUR_SQL_SERVER_PASSWORD
RACEDAY_JWT_KEY=REPLACE_WITH_A_RANDOM_SECRET_AT_LEAST_32_CHARACTERS
RACEDAY_AZURE_CONNECTION_STRING=REPLACE_WITH_YOUR_AZURE_STORAGE_CONNECTION_STRING
```

Use your own values. Never paste real passwords, JWT signing keys, or Azure connection strings into this README or commit them to GitHub. If a `.env.example` template is included in the repository, copy it to `.env` and fill in the local values.

Start the containers

Run these commands from the repository root:

```powershell
docker compose up --build -d
docker compose ps
```

The Compose setup starts the SQL Server database, the Part 2 API, and the Part 3 MVC application.

- **RaceDay MVC website:** http://localhost:8080
- **RaceDay API Swagger:** http://localhost:5285/swagger
- **SQL Server (for SSMS):** `localhost,1433`

To view container logs:

```powershell
docker compose logs -f
```

To stop the containers without deleting the database volume:

```powershell
docker compose down
```

**Important:** Do not use `docker compose down -v` for normal shutdown. The `-v` option removes Compose-managed volumes and can delete the SQL Server data stored in them. Keep the database volume when stopping or restarting the application.

Run only the MVC site in Docker (API running on the host)

This is an alternative development setup, not the full three-container demonstration. Start the API on the host first, then run:

```powershell
docker build -t raceday-mvc ./RaceDay.MVC
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5285/ raceday-mvc
```

The MVC container uses `host.docker.internal` to reach an API running on the host machine.

Run it locally (without Docker)

1. Start the API with Visual Studio (http or https profile) or `dotnet run --project RaceDay.API`.
2. Store the Azure Storage connection string for the API (it is never committed):
   ```
   dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>" --project RaceDay.API
   ```
3. `RaceDay.MVC/appsettings.json` already points at the API (`"ApiSettings": { "BaseUrl": "http://localhost:5285/" }`). Run `RaceDay.MVC`.

Part 2 feedback and demonstration evidence

The Part 2 formative feedback the lecturer specifically asked for the video presentation to show:

- API registration and login for both account roles: **Participant** and **Organiser**.
- Logout and switching between authenticated users.
- Relevant database information from SQL Server Management Studio (SSMS).

Use the following demonstration sequence when recording the Part 2/API portion of the video:

1. Open the Part 2 API Swagger page.
2. Register a Participant account and show the successful response.
3. Register an Organiser account and show the successful response.
4. Log in as the Participant and demonstrate an endpoint available to that role.
5. Log out or end that authenticated session, then log in as the Organiser.
6. Demonstrate an Organiser-only endpoint and show that role restrictions are enforced.
7. Show the logout process and explain that the authenticated session/token is ended before changing users.
8. Open SSMS and connect to the Docker SQL Server instance at `localhost,1433` using SQL Server Authentication. Use the local `sa` password stored privately in the `.env` file; never put the password in the README or video.
9. Expand `RaceDayDb` and show the database tables and relevant records. The restored database used during development contains the tables `Organiser`, `Participant`, `Event`, `Category`, `Enrolment`, and `Result`, together with the EF Core migrations history table.

For Part 3, continue the video by showing the MVC application, its role-specific navigation and features, the API/Blob Storage flow, and the application running through Docker Compose. The Part 3 video link must be added above before submission.

GitHub Actions

`.github/workflows/ci.yml` runs on every push and pull request. It validates the repository structure (and checks that no secrets file is committed),
builds the API, tests and MVC site, runs all unit tests, validates `docker-compose.yml` and builds both Docker images.

Screenshot of a successful green build:
<img width="928" height="412" alt="Screenshot 2026-11-09 161715" src="https://github.com/user-attachments/assets/543d4b27-05ea-472b-9ae8-9f98c86d09a5" />

Security notes

* Passwords are stored as salted hashes. The browser only holds a session cookie; the JWT stays on the server in the session.
* Secrets (database password, JWT key, Azure connection string) live in the git-ignored `.env` file, User Secrets or environment variables, never in the repository.
* Uploaded images are checked for type, size and real image content, and stored in private containers. Only the API can read them.
