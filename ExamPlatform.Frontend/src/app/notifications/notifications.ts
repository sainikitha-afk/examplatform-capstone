// notifications.ts — Lists all notifications for the current user and lets them mark as read.
// CONCEPTS DEMO: date pipe, *ngFor, *ngIf, event binding, [ngClass].

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationApiService } from '../services/notificationapi.service';
import { NotificationModel } from '../models/notification.model';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css',
})
export class Notifications implements OnInit {
  notifications: NotificationModel[] = [];
  isLoading: boolean = true;
  errorMessage: string = '';

  constructor(private notifService: NotificationApiService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.notifService.loadNotifications().subscribe({
      next: (result) => {
        this.notifications = result.items;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load notifications.';
        this.isLoading = false;
      }
    });
  }

  handleMarkReadClick(notif: NotificationModel): void {
    if (notif.isRead) return;
    this.notifService.markAsRead(notif.notificationId).subscribe({
      next: () => notif.isRead = true,
      error: () => alert('Could not mark as read.')
    });
  }

  handleMarkAllReadClick(): void {
    this.notifService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
      },
      error: () => alert('Failed to mark all as read.')
    });
  }
}
