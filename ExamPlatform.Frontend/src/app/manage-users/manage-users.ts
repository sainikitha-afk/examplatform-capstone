// manage-users.ts — Admin: view all users, toggle active/inactive status.
// CONCEPTS DEMO: *ngFor table, [ngClass] for active/inactive row styling, event binding.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserApiService } from '../services/userapi.service';
import { UserModel } from '../models/user.model';

@Component({
  selector: 'app-manage-users',
  imports: [CommonModule],
  templateUrl: './manage-users.html',
  styleUrl: './manage-users.css',
})
export class ManageUsers implements OnInit {
  users: UserModel[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  currentPage: number = 1;
  totalPages: number = 1;

  constructor(private userService: UserApiService) {}

  ngOnInit(): void { this.loadUsers(); }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.loadUsers(this.currentPage).subscribe({
      next: (result) => {
        this.users = result.items;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: () => { this.errorMessage = 'Failed to load users.'; this.isLoading = false; }
    });
  }

  handleToggleActiveClick(user: UserModel): void {
    const newStatus = !user.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    if (!confirm(`Are you sure you want to ${action} ${user.fullName}?`)) return;

    this.userService.toggleUserActive(user.userId, newStatus).subscribe({
      next: (updated) => {
        user.isActive = updated.isActive;
        this.successMessage = `${user.fullName} has been ${action}d.`;
      },
      error: (e) => this.errorMessage = e?.error?.message ?? 'Failed to update status.'
    });
  }

  handlePageChange(page: number): void {
    this.currentPage = page;
    this.loadUsers();
  }
}
