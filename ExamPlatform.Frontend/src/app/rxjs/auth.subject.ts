// auth.subject.ts — shared BehaviorSubject that broadcasts the currently-logged-in user.
// Why it exists: multiple components (navigation, dashboards) need the current user without
//   making another HTTP call. BehaviorSubject is chosen over plain Subject because it holds
//   the LATEST value — any new subscriber gets the current user immediately.
// How Angular uses it: AuthService pushes a new value after login/register/logout;
//   Navigation subscribes via the async pipe to update the username/role display in real time.

import { BehaviorSubject } from 'rxjs';
import { AuthResponseModel } from '../models/auth-response.model';

// Restore from sessionStorage so a page refresh doesn't lose the logged-in state.
function restoreSession(): AuthResponseModel | null {
  const token = sessionStorage.getItem('token');
  if (!token) return null;
  return new AuthResponseModel(
    token,
    Number(sessionStorage.getItem('userId') ?? '0'),
    sessionStorage.getItem('fullName') ?? '',
    sessionStorage.getItem('username') ?? '',
    sessionStorage.getItem('email') ?? '',
    sessionStorage.getItem('role') ?? ''
  );
}

// Exported as a module-level singleton. Injectable services and standalone components
// import this directly — no need for a wrapper service.
export const currentUser$ = new BehaviorSubject<AuthResponseModel | null>(restoreSession());
