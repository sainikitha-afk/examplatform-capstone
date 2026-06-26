// notification.subject.ts — app-wide Subject for fire-and-forget toast notifications.
// Why it exists: any service or component can broadcast a short message ("Exam submitted!")
//   without knowing which component will display it. Decouples the sender from the receiver.
// Why Subject (not BehaviorSubject): late subscribers should NOT receive old toasts.
//   A plain Subject only delivers messages to currently-active subscribers.
// How Angular uses it: a toast/alert bar component subscribes and renders each message;
//   services call notificationToast$.next("message") to fire a notification.

import { Subject } from 'rxjs';

export const notificationToast$ = new Subject<string>();
