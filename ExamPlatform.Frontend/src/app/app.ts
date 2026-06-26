import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Login } from './login/login';   // ← add this

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Login],         // ← add Login here
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('ExamPlatform.Frontend');
}