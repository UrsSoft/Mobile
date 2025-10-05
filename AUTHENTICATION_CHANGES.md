# Authentication System Changes

## Overview
The authentication system has been updated to use phone numbers instead of email addresses for login, and includes "Remember Me" functionality.

## Changes Made

### 1. Backend Changes

#### DTOs Updated:
- `LoginDto`: Changed from email to phone authentication
- `SupplierRegisterDto`: Made phone field required
- `CreateEmployeeDto`: Made phone field required with validation

#### Controllers Updated:
- `AuthController`: 
  - Login method now searches by phone instead of email
  - Added phone uniqueness validation in supplier registration
  - Added `create-test-users` endpoint for testing
  - Updated `reset-admin` endpoint to set default phone number
- `AdminController`: 
  - Added phone uniqueness validation in employee creation and updates

#### Models Updated:
- `User`: Made Phone field required with Phone validation attribute

### 2. Frontend Changes

#### Services Updated:
- `AuthService`: 
  - Added RememberMe support using cookies
  - Tokens and user data are stored in cookies when RememberMe is checked
  - Added automatic initialization from cookies
  - Cookies expire after 30 days

#### Controllers Updated:
- `AccountController`: 
  - Login method now accepts RememberMe parameter
  - Added cookie initialization in Login and Profile actions

#### Views Updated:
- `Login.cshtml`: 
  - Changed email input to phone input
  - Properly bound RememberMe checkbox to view model
  - Updated placeholder text and labels

#### Configuration Updated:
- `Program.cs`: Added middleware to automatically initialize authentication from cookies

## How RememberMe Works

1. **When RememberMe is checked:**
   - User credentials are stored in both session and secure HTTP-only cookies
   - Cookies are set to expire in 30 days
   - Cookies are marked as Secure and HttpOnly for security

2. **When user returns:**
   - Middleware automatically checks for auth cookies on each request
   - If valid cookies exist, user session is restored automatically
   - User remains logged in even after browser restart

3. **When user logs out:**
   - Both session data and cookies are cleared completely

## Test Users

Use the `/api/Auth/create-test-users` endpoint to create test users:

- **Admin**: Phone: `+905551234567`, Password: `admin123`
- **Employee**: Phone: `+905551234568`, Password: `employee123`  
- **Supplier**: Phone: `+905551234569`, Password: `supplier123`

## Security Features

- Cookies are marked as HttpOnly (prevents XSS attacks)
- Cookies are marked as Secure (HTTPS only)
- Cookies use SameSite=Strict (prevents CSRF attacks)
- Phone numbers must be unique across all users
- Passwords are still hashed with BCrypt

## Usage

1. Users now log in with their phone number and password
2. Check "Beni hatýrla" (Remember Me) to stay logged in for 30 days
3. System automatically restores session from cookies when available
4. All existing functionality remains the same, just with phone-based authentication