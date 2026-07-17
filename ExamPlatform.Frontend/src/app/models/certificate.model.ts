// CertificateModel — frontend twin of CandidateCertificateDto and CertificateDto.
// Why it exists: the certificates screen lists the current user's earned certificates.
// How Angular uses it: certificateapi.service.ts returns Observable<CandidateCertificateModel[]>;
// the template uses the date pipe on issuedAt to show formatted dates.

// CandidateCertificateModel — candidate's own view (no userId / attemptId).
export class CandidateCertificateModel {
  constructor(
    public certificateId: number = 0,
    public certificateCode: string = "",
    public examId: number = 0,
    public examTitle: string = "",
    public score: number = 0,
    public issuedAt: string = "",
    public certStatus: string = "",     // "Issued" | "Revoked"
    public revokedAt: string | null = null,
    // Premium / institute branding, snapshotted at issuance.
    public isPremium: boolean = false,
    public instituteName: string | null = null,
    public instituteLogoUrl: string | null = null
  ) {}
}

// CertificateModel — admin view: includes userId and attemptId.
export class CertificateModel {
  constructor(
    public certificateId: number = 0,
    public certificateCode: string = "",
    public userId: number = 0,
    public userFullName: string = "",
    public examId: number = 0,
    public examTitle: string = "",
    public attemptId: number = 0,
    public score: number = 0,
    public issuedAt: string = "",
    public certStatus: string = "",
    public revokedAt: string | null = null
  ) {}
}

// CertificateDownloadModel — returned by GET /api/certificates/{id}/download.
export class CertificateDownloadModel {
  constructor(
    public certificateId: number = 0,
    public certificateCode: string = "",
    public candidateName: string = "",
    public examTitle: string = "",
    public totalMarks: number = 0,
    public score: number = 0,
    public issuedAt: string = "",
    public certStatus: string = "",
    public certificateText: string = "",
    // Premium / institute branding for the fancy certificate design.
    public isPremium: boolean = false,
    public instituteName: string | null = null,
    public instituteLogoUrl: string | null = null
  ) {}
}
