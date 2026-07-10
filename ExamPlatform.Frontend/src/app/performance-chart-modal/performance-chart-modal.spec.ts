import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PerformanceChartModal } from './performance-chart-modal';

describe('PerformanceChartModal', () => {
  let component: PerformanceChartModal;
  let fixture: ComponentFixture<PerformanceChartModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PerformanceChartModal],
    }).compileComponents();

    fixture = TestBed.createComponent(PerformanceChartModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
