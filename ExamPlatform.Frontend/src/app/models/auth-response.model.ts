// AuthResponseModel — shape of the JSON returned by both POST /api/auth/login and POST /api/auth/register.
// Why it exists: both endpoints return the same DTO so one model covers both.
// How Angular uses it: stored in sessionStorage and broadcast via the auth BehaviorSubject so every
// component can read the current user without making another HTTP call.
export class AuthResponseModel {
  constructor(
    public token: string = "",
    public userId: number = 0,
    public fullName: string = "",
    public username: string = "",
    public email: string = "",
    public role: string = ""   // "Admin" | "Examiner" | "Candidate" — verbatim enum name from backend
  ) {}
}
