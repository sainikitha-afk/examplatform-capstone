import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LoginModel } from '../models/login.model';

@Component({
  selector: 'app-login',
  // FormsModule must be imported here for [(ngModel)] to work in the template.
  // This is the "standalone component" pattern — the component declares its own
  // dependencies, no NgModule needed.
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  // The "live" object the form is bound to.
  // Two-way binding means: type in the input → loginData.username updates instantly.
  loginData: LoginModel = new LoginModel();

  // Click handler. Mentor's naming convention: handle<What>Click().
  // For now, just log what the user typed — proves binding works.
  // In Stage C, this method will call the AuthService and hit /api/auth/login.
  handleLoginClick() {
    console.log("Login clicked!");
    console.log("Username:", this.loginData.username);
    console.log("Password:", this.loginData.password);
    alert(`Login attempt — username: ${this.loginData.username}`);
  }
}