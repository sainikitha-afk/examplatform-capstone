// LoginModel — holds the username + password the user types into the login form.
// Sent to POST /api/auth/login as the request body.
// Plain TypeScript class. Same shape your backend's LoginDto expects.
export class LoginModel {
  constructor(
    public username: string = "",
    public password: string = ""
  ) {}
}