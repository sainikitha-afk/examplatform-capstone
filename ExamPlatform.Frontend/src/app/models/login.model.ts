// LoginModel — holds the credentials the user types into the login form.
// Sent to POST /api/auth/login as the request body.
// Backend's LoginDto accepts either email or username in the emailOrUsername field.
export class LoginModel {
  constructor(
    public emailOrUsername: string = "",
    public password: string = ""
  ) {}
}