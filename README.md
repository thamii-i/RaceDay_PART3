# RaceDay

RaceDay is an event management system for South African road running, walking and cycling events. Organisers create events and capture results. Participants find events, enrol and follow their race history.

How it works
Browser → RaceDay.Web (MVC) → RaceDay.Api (Web API) → SQL Server
                                     ↓
                              wwwroot/uploads (images)
RaceDay.Api is the only part that touches the database. It holds all the business rules and handles security and image storage.
RaceDay.Web is the website. It has no database access and no business logic. It calls the API and displays the results.
RaceDay.Tests runs the real API against an in-memory database.
Database

Seven tables: Organisers, Participants, Events, Categories, EventCategories, Enrollments and Results. An enrollment links a participant, an event and the category they chose. Its status is Pending, Confirmed or Cancelled.

Security
Users register as an Organiser or a Participant. Other roles are rejected.
Passwords are stored as hashes, never as plain text.
Logging in returns a JWT carrying the user’s id and role. The website keeps it in a secure cookie session.
Wrong role or not the owner gives 403. Not logged in gives 401. Protected pages redirect to the login page.
What each role can do
Organiser	Participant
Dashboard with events, enrolments and upcoming dates	Browse and filter upcoming events
Create, edit and delete events, with a banner image	View event details and categories
Add or remove categories	Enrol with a category and get a bib number
View enrolments and confirm or cancel them	See enrolment status
Capture finish times and positions, then publish them	View published results, such as 47th out of 312
	Update profile and upload a picture
Key rules (enforced by the API)
One enrollment per participant per event, in a category that belongs to the event.
Every enrollment gets a unique bib number, and finishing positions are unique within an event.
Participants only see results after the organiser publishes them.
Images

Images go from the website to the API, which saves them in wwwroot/uploads (JPEG, PNG, WebP or GIF, up to 5 MB). Only the file path is stored in the database. Pages load images through the API.

UI and testing

The site uses Bootstrap with a navy, orange and lime sporting theme. Statuses are colour-coded: green for Confirmed, amber for Pending and red for Cancelled. Invalid input shows friendly messages, never raw errors. The xUnit tests cover success and failure cases for logins, events, role access, enrolments, results and uploads, and run on GitHub Actions.
