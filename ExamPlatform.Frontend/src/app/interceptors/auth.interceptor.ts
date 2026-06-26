// auth.interceptor.ts — HTTP interceptor that attaches the JWT to every outgoing request.
// Why it exists: without this, every service method would have to manually add the
//   Authorization header. The interceptor does it once, automatically, for all requests.
// How Angular uses it: registered as an HttpInterceptorFn in app.config.ts via
//   provideHttpClient(withInterceptors([authInterceptor])). Angular calls this function
//   before every HTTP request leaves the browser.
// CONCEPT DEMO: HttpInterceptorFn — a functional (non-class) interceptor supported in Angular 15+.

import { HttpInterceptorFn } from '@angular/common/http';

// authInterceptor is a plain function — no class, no @Injectable needed.
// Angular's functional interceptor API passes (req, next) as arguments.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = sessionStorage.getItem('token');

  // If no token is stored (user is not logged in), forward the original request unchanged.
  if (!token) {
    return next(req);
  }

  // Clone the request and add the Authorization header.
  // Requests are immutable in Angular — cloning is required.
  const authenticatedReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authenticatedReq);
};
