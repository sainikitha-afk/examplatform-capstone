// RegisterModel — shape of the request body sent to POST /api/auth/register.
// Why it exists: keeps the register form data in a typed object instead of a loose object literal.
// How Angular uses it: the register component creates an instance and binds the form to it via [(ngModel)].
export class RegisterModel {
  constructor(
    public fullName: string = "",
    public username: string = "",
    public email: string = "",
    public password: string = ""
  ) {}
}
