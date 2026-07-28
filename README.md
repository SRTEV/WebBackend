# SRTEV — Web Backend API

A RESTful Web API built with **C# .NET** and **MySQL** that powers the SRTEV (Electric Vehicle Rental Platform). The API handles user management, vehicle tracking, rental sessions, payment processing, route history, and customer support ticket resolutions with automated email notifications.

🔗 **Frontend Dashboard:** [SRTEV Web Frontend](https://github.com/SRTEV/WebFrontend)

---

## 🛠️ Tech Stack

* **Framework:** .NET 8 / ASP.NET Core Web API
* **Database:** MySQL / MariaDB
* **ORM / Database Access:** Entity Framework Core
* **Authentication:** JWT (JSON Web Tokens) & Role-based Authorization
* **Mailing Service:** MailKit / MimeKit (SMTP Integration)

---

## ✨ Features & Modules

* **User & Role Management:**
  * User registration, authentication, JWT issuing, and profile control.
  * Role-based access control (Admin, User, Repairman).
  * Password reset workflow via tokenized email links.

* **Fleet & Rental System:**
  * Vehicle status tracking (Battery level, GPS coordinates, vehicle type).
  * Rental plans and active rental sessions.
  * Route history tracking with live coordinates and speed recording.

* **Operations & Ticketing (Support Reports):**
  * Ticket creation for vehicle breakdown, payment issues, or account problems.
  * Support reply handling with automatic SMTP email dispatching to users.

* **Payments & Rewards:**
  * Payment status tracking and user balance management.
  * Competition and user achievement/ranking tracking.

---

## 🗄️ Database Architecture

The system utilizes a relational MySQL schema (`diplom`) comprising:
* **Users & Access:** `User`, `Role`, `Card`
* **Fleet Management:** `Vehicle`, `Vehicle_Type`, `Vehicle_Status`, `Zone`
* **Rentals & Routes:** `Rental`, `Rental_Plan`, `Route_History`
* **Finance:** `Payment`
* **Ticketing & Logs:** `Report`, `Report_response`, `Notifications`
* **Gamification:** `Competition`, `Users_result`, `Goal_type`, `Reward_type`

---

## 🚀 Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) (v8.0 or higher)
* [MySQL Server](https://www.mysql.com/) or MariaDB
* SMTP credentials or a mock SMTP service like [Ethereal Email](https://ethereal.email)

---

### Configuration

1. **Clone the repository:**

```bash
git clone https://github.com/SRTEV/WebBackend.git
cd WebBackend
```

2. **Configure `appsettings.json`:**
Set up your MySQL connection string, JWT secrets, and SMTP settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=diplom;User=root;Password=your_password;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyHere",
    "Issuer": "SRTEVBackend",
    "Audience": "SRTEVFrontend"
  },
  "EMAIL_FROM": "joaquin.gerhold@ethereal.email",
  "EMAIL_USERNAME": "joaquin.gerhold@ethereal.email",
  "EMAIL_PASSWORD": "your_smtp_password"
}
```

3. **Initialize Database:**
Import the database schema using your SQL script or run Entity Framework migrations:

```bash
dotnet ef database update
```

4. **Run the Application:**

```bash
dotnet run
```

The API will start at `http://localhost:5194` (or your configured port). Swagger UI will be accessible at `/swagger`.

---

## 🔗 Key API Endpoints

* `GET /api/Report` — Fetch all user & repairman support reports.
* `POST /api/Report/{id}/reply` — Send an email reply to a support report.
* `POST /api/Auth/login` — Authenticate user and issue JWT.
* `GET /api/Vehicle` — Retrieve fleet status and locations.

---

## 🔗 Connected Frontend

* 📁 **Frontend Repository:** [https://github.com/SRTEV/WebFrontend](https://github.com/SRTEV/WebFrontend)
