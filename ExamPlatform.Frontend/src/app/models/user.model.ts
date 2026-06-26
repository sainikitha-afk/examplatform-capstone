// UserModel — frontend twin of UserSummaryDto / UserDetailDto from the backend.
// Why it exists: Admin screens (manage-users, manage-examiners) need a typed object for each user row.
// How Angular uses it: userapi.service.ts returns Observable<UserModel[]> and the component renders
// them in *ngFor tables.
export class UserModel {
  constructor(
    public userId: number = 0,
    public fullName: string = "",
    public username: string = "",
    public email: string = "",
    public role: string = "",       // "Admin" | "Examiner" | "Candidate"
    public isActive: boolean = true,
    public dateOfJoining: string = ""
  ) {}
}

// CreateExaminerModel — request body for POST /api/users/examiners (Admin only).
export class CreateExaminerModel {
  constructor(
    public fullName: string = "",
    public username: string = "",
    public email: string = "",
    public password: string = ""
  ) {}
}
