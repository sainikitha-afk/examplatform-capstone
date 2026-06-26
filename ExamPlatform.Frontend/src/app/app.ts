// app.ts — Root component: hosts the navigation bar and the router outlet.
// Why it exists: every Angular app has one root component that is bootstrapped in main.ts.
//   All other components are rendered inside <router-outlet> based on the current URL.
// CONCEPTS DEMO: RouterOutlet, standalone component with imported child components.

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navigation } from './navigation/navigation';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navigation],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
