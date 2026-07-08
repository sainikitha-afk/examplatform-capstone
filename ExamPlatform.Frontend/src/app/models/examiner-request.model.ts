// Models for POST /api/examiner-requests (submit) and admin review endpoints.

export class SubmitExaminerRequestModel {
  fullName: string = '';
  email:    string = '';
  purpose:  string = '';
}

export class ExaminerRequestModel {
  examinerRequestId: number   = 0;
  fullName:          string   = '';
  email:             string   = '';
  purpose:           string   = '';
  status:            string   = '';  // 'Pending' | 'Approved' | 'Rejected'
  requestedAt:       string   = '';
  processedAt:       string   | null = null;
  adminNote:         string   | null = null;
}
