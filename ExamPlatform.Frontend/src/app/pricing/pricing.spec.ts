// pricing.spec.ts — public pricing page: loads plans, renders all three tiers,
// and the Professional CTA adapts to login/role state.
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Pricing } from './pricing';

describe('Pricing', () => {
  let fixture: ComponentFixture<Pricing>;
  let component: Pricing;
  let httpMock: HttpTestingController;
  const url = 'http://localhost:5014/api/subscriptions/plans';

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [Pricing],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Pricing);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  function flushPlans() {
    fixture.detectChanges();
    httpMock.expectOne(url).flush([
      { planId: 1, name: 'Free Trial', tier: 'Trial', priceMonthly: 0, priceYearly: 0, maxExamsLifetime: 8, examCooldownDays: null },
      { planId: 2, name: 'Professional', tier: 'Professional', priceMonthly: 1499, priceYearly: 14990, maxExamsLifetime: null, examCooldownDays: null },
      { planId: 3, name: 'Free Forever', tier: 'FreeForever', priceMonthly: 0, priceYearly: 0, maxExamsLifetime: null, examCooldownDays: 3 },
    ]);
    fixture.detectChanges();
  }

  it('loads and renders all three plan tiers', () => {
    flushPlans();

    expect(component.plans.length).toBe(3);
    expect(fixture.nativeElement.textContent).toContain('Free Trial');
    expect(fixture.nativeElement.textContent).toContain('Professional');
    expect(fixture.nativeElement.textContent).toContain('Free Forever');
  });

  it('shows an error message when the load fails', () => {
    fixture.detectChanges();
    httpMock.expectOne(url).flush('error', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.errorMessage).toContain('Could not load');
  });

  it('routes an anonymous visitor to login on the Professional CTA', () => {
    const routerSpy = vi.spyOn((component as any).router, 'navigate');
    flushPlans();

    component.handleProfessionalCtaClick();

    expect(routerSpy).toHaveBeenCalledWith(['/login']);
  });

  it('routes a signed-in examiner to their subscription dashboard', () => {
    sessionStorage.setItem('token', 'fake');
    sessionStorage.setItem('role', 'Examiner');
    const routerSpy = vi.spyOn((component as any).router, 'navigate');
    flushPlans();

    component.handleProfessionalCtaClick();

    expect(routerSpy).toHaveBeenCalledWith(['/examiner/subscription']);
  });
});
