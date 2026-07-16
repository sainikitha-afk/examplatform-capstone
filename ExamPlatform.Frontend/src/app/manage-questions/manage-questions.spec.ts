// manage-questions.spec.ts — unit tests for the Question Bank server-side
// pagination logic: envelope capture, filter/search resets to page 1, bounds
// checking, duplicate-request guarding, the "Showing X–Y of Z" math, and the
// delete-last-item-on-page step-back. Direct instantiation with mocked services
// (constructor injection) — no TestBed needed.
import { of } from 'rxjs';
import { ManageQuestions } from './manage-questions';
import { QuestionModel } from '../models/question.model';

function q(id: number): QuestionModel {
  return new QuestionModel(id, 10, 1, `Question ${id}?`, 5, 'Easy');
}

// Build a paged envelope for a given page.
function page(items: QuestionModel[], pageNum: number, totalCount: number, pageSize = 10) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  return {
    items, page: pageNum, pageSize, totalCount, totalPages,
    hasNextPage: pageNum < totalPages,
    hasPreviousPage: pageNum > 1,
  };
}

describe('ManageQuestions (pagination logic)', () => {
  let component: ManageQuestions;
  let questionService: any;
  let categoryService: any;

  beforeEach(() => {
    questionService = {
      loadQuestions: vi.fn(() => of(page([q(1), q(2)], 1, 25))),
      deleteQuestion: vi.fn(() => of(void 0)),
    };
    categoryService = { loadCategories: vi.fn(() => of([])) };
    component = new ManageQuestions(questionService, categoryService);
  });

  it('captures the full pagination envelope from the response', () => {
    component.loadQuestions();
    expect(component.totalCount).toBe(25);
    expect(component.totalPages).toBe(3);
    expect(component.hasNextPage).toBe(true);
    expect(component.hasPreviousPage).toBe(false);
    expect(component.questions.length).toBe(2);
  });

  it('requests the current page and size (10) from the backend, not the whole bank', () => {
    component.page = 2;
    component.filterDifficulty = 'Easy';
    component.loadQuestions();
    // signature: (catId, difficulty, search, page, pageSize)
    expect(questionService.loadQuestions).toHaveBeenCalledWith(null, 'Easy', '', 2, 10);
  });

  it('onFilterChange resets to page 1', () => {
    component.page = 3;
    component.onFilterChange();
    expect(component.page).toBe(1);
    expect(questionService.loadQuestions).toHaveBeenCalledWith(null, '', '', 1, 10);
  });

  it('goToPage ignores out-of-range targets and the current page', () => {
    questionService.loadQuestions.mockReturnValue(of(page([q(1)], 2, 25)));
    component.loadQuestions();          // page 1 → totalPages 3
    questionService.loadQuestions.mockClear();

    component.goToPage(0);              // below range
    component.goToPage(99);            // above range
    component.goToPage(1);            // same as current
    expect(questionService.loadQuestions).not.toHaveBeenCalled();

    component.goToPage(2);             // valid
    expect(component.page).toBe(2);
    expect(questionService.loadQuestions).toHaveBeenCalledTimes(1);
  });

  it('nextPage / prevPage respect hasNextPage / hasPreviousPage', () => {
    component.loadQuestions();          // page 1 of 3
    expect(component.hasPreviousPage).toBe(false);
    component.prevPage();               // no-op on page 1
    expect(component.page).toBe(1);
    component.nextPage();               // → page 2
    expect(component.page).toBe(2);
  });

  it('guards against duplicate requests while one is in flight', () => {
    // A never-completing observable keeps isPaging=true.
    questionService.loadQuestions.mockReturnValue({ subscribe: () => {} } as any);
    component.loadQuestions();          // starts, stays "paging"
    component.loadQuestions();          // should be ignored
    expect(questionService.loadQuestions).toHaveBeenCalledTimes(1);
  });

  it('showingFrom / showingTo compute the visible range', () => {
    questionService.loadQuestions.mockReturnValue(of(page([q(1)], 3, 25)));
    component.page = 3;
    component.loadQuestions();
    expect(component.showingFrom).toBe(21); // (3-1)*10 + 1
    expect(component.showingTo).toBe(25);   // min(30, 25)
  });

  it('deleting the last row on a page steps back a page', () => {
    vi.stubGlobal('confirm', vi.fn(() => true));
    // On page 2 with a single visible row.
    questionService.loadQuestions.mockReturnValue(of(page([q(11)], 2, 11)));
    component.page = 2;
    component.loadQuestions();
    expect(component.questions.length).toBe(1);

    component.handleDeleteClick(11);
    // After delete of the only row, we drop to page 1 and reload.
    expect(component.page).toBe(1);
    vi.unstubAllGlobals();
  });
});
