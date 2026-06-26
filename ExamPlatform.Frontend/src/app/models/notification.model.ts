// NotificationModel — frontend twin of NotificationDto from the backend.
// Why it exists: the notifications screen lists messages sent to the current user
// (e.g., "Your result for Exam X has been published").
// How Angular uses it: notificationapi.service.ts returns Observable<NotificationModel[]>;
// the template uses the date pipe on createdAt and *ngIf on isRead for styling.
export class NotificationModel {
  constructor(
    public notificationId: number = 0,
    public notifType: string = "",         // "ResultPublished" | "CertificateIssued" | etc.
    public message: string = "",
    public relatedExamId: number | null = null,
    public isRead: boolean = false,
    public createdAt: string = ""
  ) {}
}

// UnreadCountModel — returned by GET /api/notifications/unread-count.
export class UnreadCountModel {
  constructor(
    public count: number = 0
  ) {}
}
