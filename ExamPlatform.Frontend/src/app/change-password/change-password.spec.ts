// change-password.spec.ts — the self-service password screen.
//
// The behaviours worth pinning: client-side validation blocks bad requests before
// they are sent, whitespace from a pasted temp password is trimmed away, a wrong
// current password shows the backend's message (no logout, no raw error), and
// success clears the form.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { ChangePassword } from './change-password';

describe('ChangePassword', () => {
  let fixture: ComponentFixture<ChangePassword>;
  let component: ChangePassword;
  let httpMock: HttpTestingController;
  const url = 'http://localhost:5014/api/auth/change-password';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChangePassword],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ChangePassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function fill(current: string, next: string, confirm: string): void {
    component.currentPassword = current;
    component.newPassword = next;
    component.confirmPassword = confirm;
    fixture.detectChanges();
  }

  it('blocks submission until the form is valid', () => {
    fill('', '', '');
    expect(component.canSubmit).toBe(false);

    fill('TempPass99', 'short', 'short');            // too short
    expect(component.canSubmit).toBe(false);

    fill('TempPass99', 'MyNewSecret1', 'different'); // mismatch
    expect(component.canSubmit).toBe(false);

    fill('TempPass99', 'TempPass99', 'TempPass99');  // same as current
    expect(component.canSubmit).toBe(false);
    expect(component.sameAsCurrent).toBe(true);

    fill('TempPass99', 'MyNewSecret1', 'MyNewSecret1');
    expect(component.canSubmit).toBe(true);

    // Nothing was sent while invalid.
    httpMock.expectNone(url);
  });

  it('sends trimmed values — a temp password pasted with whitespace still works', () => {
    fill(' TempPass99\n', '  MyNewSecret1  ', '  MyNewSecret1  ');

    component.handleSubmit();

    const req = httpMock.expectOne(url);
    expect(req.request.body).toEqual({
      currentPassword: 'TempPass99',
      newPassword: 'MyNewSecret1',
    });
    req.flush({ message: 'Password changed successfully.' });
  });

  it('shows success and clears the form fields', () => {
    fill('TempPass99', 'MyNewSecret1', 'MyNewSecret1');

    component.handleSubmit();
    httpMock.expectOne(url).flush({ message: 'Password changed successfully.' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Password changed successfully');
    expect(component.currentPassword).toBe('');
    expect(component.newPassword).toBe('');
    expect(component.confirmPassword).toBe('');
  });

  it('surfaces the backend message for a wrong current password (400, no logout)', () => {
    fill('wrong-guess', 'MyNewSecret1', 'MyNewSecret1');

    component.handleSubmit();
    httpMock.expectOne(url).flush(
      { status: 400, error: 'Current password is incorrect.' },
      { status: 400, statusText: 'Bad Request' }
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Current password is incorrect.');
    expect(component.isLoading).toBe(false);
  });

  it('never renders a raw backend error body', () => {
    fill('TempPass99', 'MyNewSecret1', 'MyNewSecret1');

    component.handleSubmit();
    httpMock.expectOne(url).flush('NullReferenceException at AuthService.line42', {
      status: 500,
      statusText: 'Server Error',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('NullReferenceException');
    expect(fixture.nativeElement.textContent).toContain('Could not change your password');
  });
});
