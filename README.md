
# 🍕 DominosApp — Pizza Ordering Web API

A full-stack pizza ordering application built with **ASP.NET Core 8.0 Web API** and a vanilla JavaScript frontend. Developed as a final project for the ASP.NET MVC course at SoftUni Buditel.

---

## Project Description

DominosApp allows users to browse a pizza menu, place orders, and track their order history. Administrators have a dedicated panel to manage users, orders, and pizzas.

---

## Architecture

The solution is separated into **5 projects** following clean architecture principles:

| Project | Responsibility |
|---|---|
| **DominosApp** | Web API controllers, middleware, frontend (wwwroot) |
| **DominosApp.Core** | Business logic — services, interfaces, view models |
| **DominosApp.Infrastructure** | Data layer — EF Core, DbContext, entity models, repository |
| **DominosApp.Util** | Utility classes — custom logger |
| **DominosApp.Tests** | NUnit unit tests with Moq mocking |

---

## Technologies Used

- ASP.NET Core 8.0 Web API
- Entity Framework Core 8.0
- Microsoft SQL Server
- ASP.NET Identity
- JWT Bearer Authentication
- NUnit + Moq (unit tests)
- HTML5 / CSS3 / Vanilla JavaScript (frontend)

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | /api/pizza | Public | Get all available pizzas |
| GET | /api/pizza/{id} | Public | Get pizza by ID |
| POST | /api/pizza | Admin | Add new pizza |
| PUT | /api/pizza/{id} | Admin | Update pizza |
| DELETE | /api/pizza/{id} | Admin | Remove pizza |
| POST | /api/auth/register | Public | Register new user |
| POST | /api/auth/login | Public | Login and get JWT token |
| GET | /api/order | Admin | Get all orders |
| GET | /api/order/my | User | Get current user orders |
| GET | /api/order/{id} | User | Get order by ID |
| POST | /api/order | User | Place new order |
| DELETE | /api/order/{id} | Admin | Delete order |
| GET | /api/admin/users | Admin | Get all users |
| GET | /api/admin/users/{id} | Admin | Get user by ID |
| POST | /api/admin/users | Admin | Create user |
| PUT | /api/admin/users/{id} | Admin | Update user |
| DELETE | /api/admin/users/{id} | Admin | Delete user |
| GET | /api/health | Public | Health check |

---

## Default Admin Account
- Email: `admin@dominos.com`
- Password: `Admin123!`

---

