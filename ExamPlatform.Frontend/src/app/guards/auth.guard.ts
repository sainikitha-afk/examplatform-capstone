// auth.guard.ts — route guard that blocks unauthenticated users.
// Why it exists: any route that requires login should redirect to /login if no JWT is stored.
//   Guards protect routes at the routing layer — not in the component itself.
// How Angular uses it: registered as canActivate: [authGuard] in app.routes.ts.
//   Angular calls this function before the component is loaded.
// CONCEPT DEMO: CanActivateFn — Angular's functional guard API (no class needed).

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

// authGuard is a plain function matching the CanActivateFn type signature.
// It uses inject() ONLY here in the guard because functional guards require it —
// the class-based constructor pattern does not apply to standalone functions.
export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = sessionStorage.getItem('token');

  if (token) {
    return true;  // User is logged in — allow navigation.
  }

  // Not logged in — redirect to login page.
  return router.createUrlTree(['/login']);
};
