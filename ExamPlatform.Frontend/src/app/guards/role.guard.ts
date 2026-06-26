// role.guard.ts — route guard that enforces role-based access control.
// Why it exists: after login, a Candidate should not be able to navigate to /admin;
//   the guard checks the stored role against the route's required role.
// How Angular uses it: registered as canActivate: [authGuard, roleGuard] in app.routes.ts.
//   Routes declare which role is required via  data: { role: 'Admin' | 'Examiner' | 'Candidate' }.
// CONCEPT DEMO: CanActivateFn with route.data — reads metadata from the route definition.

import { inject } from '@angular/core';
import { CanActivateFn, ActivatedRouteSnapshot, Router } from '@angular/router';

// roleGuard checks route.data['role'] against the value stored in sessionStorage after login.
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const router = inject(Router);
  const requiredRole = route.data['role'] as string | undefined;

  // If the route has no role requirement, allow any authenticated user through.
  if (!requiredRole) return true;

  const userRole = sessionStorage.getItem('role');
  if (userRole === requiredRole) return true;

  // Role mismatch — send the user to their own dashboard.
  switch (userRole) {
    case 'Admin':     return router.createUrlTree(['/admin']);
    case 'Examiner':  return router.createUrlTree(['/examiner']);
    default:          return router.createUrlTree(['/candidate']);
  }
};
