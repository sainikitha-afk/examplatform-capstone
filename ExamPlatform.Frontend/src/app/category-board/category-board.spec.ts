import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CategoryBoard } from './category-board';
import { CategoryModel } from '../models/category.model';

describe('CategoryBoard', () => {
  let component: CategoryBoard;
  let fixture: ComponentFixture<CategoryBoard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryBoard],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryBoard);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('emits null when the "All Categories" board is clicked', () => {
    let emitted: number | null | undefined;
    component.categorySelected.subscribe(v => (emitted = v));

    component.select(null);

    expect(emitted).toBeNull();
  });

  it('emits the category id when a category board is clicked', () => {
    let emitted: number | null | undefined;
    component.categorySelected.subscribe(v => (emitted = v));

    component.select(3);

    expect(emitted).toBe(3);
  });

  it('sums published exam counts across categories for the "All" board', () => {
    component.categories = [
      new CategoryModel(1, 'C#', 'desc', 5),
      new CategoryModel(2, 'SQL', 'desc', 2),
    ];

    expect(component.totalCount()).toBe(7);
  });

  it('renders one board card per category plus the "All" board', () => {
    component.categories = [
      new CategoryModel(1, 'C#', 'desc', 5),
      new CategoryModel(2, 'SQL', 'desc', 2),
    ];
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.board-card');
    expect(cards.length).toBe(3); // All + 2 categories
  });

  it('assigns a deterministic icon style based on category id', () => {
    const style1 = component.styleFor(1);
    const style2 = component.styleFor(1);
    expect(style1).toEqual(style2);
  });
});
