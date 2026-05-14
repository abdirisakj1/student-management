# Smart Waste Management API

ASP.NET Core 10 Web API with **MongoDB**, **JWT authentication**, **Swagger**, and role-based authorization (**Admin**, **User**, **TruckDriver**).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) running locally (default `mongodb://localhost:27017`) or update `MongoDbSettings` in `appsettings.json`.

## Run

```bash
dotnet restore
dotnet run --project SmartWasteManagement.csproj
```

If `dotnet build` fails because `SmartWasteManagement.exe` is locked, stop the running API (stop debugging or end the `SmartWasteManagement` process), then build again.

- HTTP: `http://localhost:5087` (see `Properties/launchSettings.json`)
- Swagger UI (Development): `/swagger`

## Configuration

| Section | Purpose |
|--------|---------|
| `MongoDbSettings` | Connection string, database name, collection names for Users, Trucks, Places |
| `Jwt` | Signing `Key` (use a long random secret in production), `Issuer`, `Audience`, `ExpiryMinutes` |

## Roles and access

| Endpoint area | Rules |
|---------------|--------|
| `POST /api/auth/register`, `POST /api/auth/login` | Public |
| `GET /api/users` | **Admin** |
| `GET /api/users/{id}` | **Admin** or the same user as the JWT subject |
| `DELETE /api/users/{id}` | **Admin** |
| `GET /api/trucks`, `GET /api/trucks/{id}` | Any authenticated user |
| `POST/PUT/DELETE /api/trucks` | **Admin** |
| `GET /api/places`, `GET /api/places/{id}` | Any authenticated user |
| `POST /api/places` | **User**, **Admin**, or **TruckDriver** |
| `PUT/DELETE /api/places` | **Admin** |

New registrations receive role **User**. To create an **Admin** (or **TruckDriver**), set the user’s **`role`** field in MongoDB to `Admin` or `TruckDriver` (any casing such as `admin` is accepted on login and normalized to `Admin` in the JWT).

**Trucks:** `POST /api/trucks` (and PUT/DELETE) require an **Admin** JWT. After changing role in MongoDB, call **login** again and use the new token.

**Places:** `POST /api/places` works for **User**, **Admin**, or **TruckDriver** (with a valid token for that account).

## Collections

- **Users** — accounts (passwords stored as BCrypt hashes).
- **Trucks** — fleet vehicles.
- **Places** — waste locations / reports.

## HTTP file

Use `SmartWasteManagement.http` in Visual Studio or REST Client extensions; set `token` and `userId` after login/register.

---

## Using this API in Postman

### 1. Start the API and MongoDB

Run `dotnet run --project SmartWasteManagement.csproj` and keep it running. Ensure MongoDB is reachable using your `MongoDbSettings` connection string.

Default base URL: **`http://localhost:5087`**

### 2. Create a Postman Environment (recommended)

1. In Postman, open **Environments** → **Create Environment**.
2. Add variables:

| Variable   | Initial value              | Use |
|-----------|-----------------------------|-----|
| `baseUrl` | `http://localhost:5087`     | All requests |
| `token`   | *(leave empty at first)*  | Filled after login |

3. Save the environment and **select it** in the top-right dropdown.

Use `{{baseUrl}}` in every request URL, for example: `{{baseUrl}}/api/auth/login`.

### 3. Register or log in (no auth header)

**Register** — `POST {{baseUrl}}/api/auth/register`

- **Headers:** `Content-Type` = `application/json`
- **Body** → **raw** → **JSON**, for example:

```json
{
  "fullName": "Jane Citizen",
  "email": "jane@example.com",
  "password": "password123",
  "phone": "+1234567890"
}
```

**Login** — `POST {{baseUrl}}/api/auth/login`

```json
{
  "email": "jane@example.com",
  "password": "password123"
}
```

The JSON response includes **`token`** and **`user`** (with `id`, `role`, etc.).

### 4. Save the JWT for other requests

**Option A — copy manually**

1. Send **Login** (or **Register**).
2. From the response body, copy the value of **`token`** (the long string only, no quotes).
3. Set environment variable **`token`** to that value, or paste it into each request as described below.

**Option B — Postman collection / folder auth**

1. Create or open a **Collection** for this API.
2. Open the collection → **Authorization** tab.
3. Type: **Bearer Token**.
4. Token: `{{token}}`.
5. After login, set `{{token}}` in the environment (or use a **Tests** script on the Login request to auto-save — see optional script below).

**Option C — per-request header**

On any protected request, tab **Authorization** → **Bearer Token** → paste the token, or use `{{token}}` if the environment variable is set.

### 5. Optional: auto-save token after login (Tests script)

On the **Login** request, open the **Tests** tab and add:

```javascript
var json = pm.response.json();
if (json.token) {
  pm.environment.set("token", json.token);
}
```

Send the request again; Postman will store `token` in the active environment.

### 6. Calling protected endpoints

All routes except **`POST /api/auth/register`** and **`POST /api/auth/login`** need a valid JWT.

Examples (with Bearer auth using `{{token}}`):

| Method | URL | Notes |
|--------|-----|--------|
| `GET` | `{{baseUrl}}/api/users` | **Admin** only |
| `GET` | `{{baseUrl}}/api/users/{{userId}}` | **Admin** or user whose `id` matches the token’s user |
| `DELETE` | `{{baseUrl}}/api/users/{{userId}}` | **Admin** only |
| `GET` | `{{baseUrl}}/api/trucks` | Any authenticated user |
| `POST` | `{{baseUrl}}/api/trucks` | **Admin** only — JSON body with `truckNumber`, `driverName`, `status`, `area` (omit `id`) |
| `GET` | `{{baseUrl}}/api/places` | Any authenticated user |
| `POST` | `{{baseUrl}}/api/places` | **User**, **Admin**, or **TruckDriver** — JSON with `title`, `description`, `location`, `imageUrl`, `status` |

Always set **Body** type to **raw** + **JSON** for `POST`/`PUT`, and **`Content-Type: application/json`** if Postman does not set it automatically.

### 7. Testing as Admin in Postman

New accounts from **Register** are **User**, so `GET /api/users` will return **403 Forbidden** until you have an **Admin** JWT.

To get an Admin token:

1. Open MongoDB Compass or `mongosh`, database **`SmartWasteDb`** (or your `DatabaseName`), collection **`Users`**.
2. Find your user document and set **`role`** to exactly **`Admin`** (same spelling as in code).
3. Call **Login** again in Postman and save the new **`token`**.

Then `GET {{baseUrl}}/api/users` with Bearer auth should return **200** with the user list.

### 8. Common issues

| Problem | What to check |
|---------|----------------|
| **401 Unauthorized** | Token missing, expired, or wrong **Bearer** value. Log in again and refresh `{{token}}`. |
| **403 Forbidden** | Your role is not allowed for this route (e.g. non-admin calling `GET /api/users`). |
| **Connection refused** | API not running, or wrong **port** in `baseUrl` (compare with `launchSettings.json`). |
| **Empty or 5xx errors** | MongoDB not running or wrong connection string in `appsettings.json`. |
