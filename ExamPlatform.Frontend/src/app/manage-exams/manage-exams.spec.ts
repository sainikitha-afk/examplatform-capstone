// manage-exams.spec.ts — unit tests for the Examiner draft workspace logic:
// multi-category selection, marks arithmetic, the attach cap, selection
// preservation across filters, publish readiness, and double-click guards.
// The component is instantiated directly with mocked services (constructor
// injection makes this possible without TestBed).
import { of } from 'rxjs';
import { ManageExams } from './manage-exams';
import { ExamDetailModel, ExamQuestionFullModel, ExamSummaryModel } from '../models/exam.model';
import { QuestionModel } from '../models/question.model';
import { CategoryModel } from '../models/category.model';

// ─── Test data builders ────────────────────────────────────────────────
function bankQuestion(id: number, marks: number, catId = 1): QuestionModel {
  return new QuestionModel(id, 10, catId, `Question ${id}?`, marks, 'Easy');
}

function attachedQuestion(id: number, marks: number, catId = 1): ExamQuestionFullModel {
  return new ExamQuestionFullModel(id, id, id, `Attached ${id}?`, catId, marks, 'Easy');
}

function draftExam(totalMarks: number, attached: ExamQuestionFullModel[] = []): ExamDetailModel {
  const exam = new ExamDetailModel(1, 'Test Exam');
  exam.totalMarks = totalMarks;
  exam.examStatus = 'Draft';
  exam.questions = attached;
  return exam;
}

describe('ManageExams (attach-questions logic)', () => {
  let component: ManageExams;
  let examService: any;
  let questionService: any;
  let categoryService: any;
  let router: any;

  beforeEach(() => {
    examService = {
      loadMyExams:    vi.fn(() => of({ items: [], totalPages: 1 })),
      loadExamById:   vi.fn(() => of(draftExam(20))),
      attachQuestions: vi.fn(() => of(draftExam(20, [attachedQuestion(1, 10)]))),
      publishExam:    vi.fn(() => of(new ExamSummaryModel())),
      unpublishExam:  vi.fn(() => of(new ExamSummaryModel())),
      publishResults: vi.fn(() => of(new ExamSummaryModel())),
      detachQuestion: vi.fn(() => of(draftExam(20))),
    };
    questionService = { loadQuestions: vi.fn(() => of({ items: [], totalPages: 1 })) };
    categoryService = { loadCategories: vi.fn(() => of([
      new CategoryModel(1, 'C# & .NET'),
      new CategoryModel(2, 'DBMS'),
    ])) };
    router = { navigate: vi.fn() };

    component = new ManageExams(examService, questionService, categoryService, router);
    component.categories = [new CategoryModel(1, 'C# & .NET'), new CategoryModel(2, 'DBMS')];
    // Suppress confirm() dialogs — every destructive action asks first.
    vi.stubGlobal('confirm', vi.fn(() => true));
  });

  afterEach(() => vi.unstubAllGlobals());

  // ─── Marks arithmetic ───────────────────────────────────────────────

  it('computes attached, selected, projected, and remaining marks', () => {
    component.selectedExam = draftExam(20, [attachedQuestion(1, 7), attachedQuestion(2, 5)]);
    component.selectedQuestions.set(3, bankQuestion(3, 6));

    expect(component.getAttachedMarks()).toBe(12);
    expect(component.getSelectedMarks()).toBe(6);
    expect(component.getProjectedTotal()).toBe(18);
    expect(component.getRemainingMarks()).toBe(2);
  });

  // ─── Selection cap ──────────────────────────────────────────────────

  it('blocks selecting a question that would exceed the exam total', () => {
    // 12 attached + 6 selected = 18 of 20 → a 3-mark question must be refused.
    component.selectedExam = draftExam(20, [attachedQuestion(1, 12)]);
    component.selectedQuestions.set(2, bankQuestion(2, 6));

    component.toggleQuestionSelect(bankQuestion(3, 3));

    expect(component.isSelected(3)).toBe(false);
    expect(component.errorMessage).toContain('exceed 20 marks');
    expect(component.errorMessage).toContain('Only 2 marks remain');
  });

  it('allows selecting a question that exactly fills the remaining marks', () => {
    component.selectedExam = draftExam(20, [attachedQuestion(1, 12)]);
    component.selectedQuestions.set(2, bankQuestion(2, 6));

    component.toggleQuestionSelect(bankQuestion(3, 2)); // exactly 2 remain

    expect(component.isSelected(3)).toBe(true);
    expect(component.getRemainingMarks()).toBe(0);
  });

  it('deselecting removes the question and clears the error', () => {
    component.selectedExam = draftExam(20);
    const q = bankQuestion(3, 5);
    component.toggleQuestionSelect(q);
    expect(component.isSelected(3)).toBe(true);

    component.toggleQuestionSelect(q);
    expect(component.isSelected(3)).toBe(false);
  });

  // ─── Duplicate / already-attached protection ────────────────────────

  it('never selects a question that is already attached to the exam', () => {
    component.selectedExam = draftExam(20, [attachedQuestion(5, 10)]);

    component.toggleQuestionSelect(bankQuestion(5, 10));

    expect(component.isAttached(5)).toBe(true);
    expect(component.isSelected(5)).toBe(false);
  });

  // ─── Selection preservation across filters ──────────────────────────

  it('preserves selections when the category filter changes', () => {
    component.selectedExam = draftExam(30);
    component.toggleQuestionSelect(bankQuestion(1, 5, 1)); // category 1

    // Switch the bank to category 2 — the bank list changes entirely.
    component.bankCatId = 2;
    questionService.loadQuestions.mockReturnValue(
      of({ items: [bankQuestion(9, 5, 2)], totalPages: 1 }));
    component.handleBankFilterChange();

    // The category-1 selection must survive, and its marks still count.
    expect(component.isSelected(1)).toBe(true);
    expect(component.getSelectedMarks()).toBe(5);
  });

  it('supports selecting questions from multiple categories', () => {
    component.selectedExam = draftExam(30);
    component.toggleQuestionSelect(bankQuestion(1, 5, 1));
    component.toggleQuestionSelect(bankQuestion(2, 5, 2));

    expect(component.selectedQuestions.size).toBe(2);
    expect(component.getSelectedMarks()).toBe(10);
  });

  // ─── Attach action ──────────────────────────────────────────────────

  it('refuses to attach when nothing is selected', () => {
    component.selectedExam = draftExam(20);

    component.handleAttachClick();

    expect(component.errorMessage).toContain('at least one');
    expect(examService.attachQuestions).not.toHaveBeenCalled();
  });

  it('attaches the selected ids, clears the selection, and stays in the workspace', () => {
    component.selectedExam = draftExam(20);
    component.editingExamId = 1;
    component.view = 'questions';
    component.toggleQuestionSelect(bankQuestion(1, 10));

    component.handleAttachClick();

    expect(examService.attachQuestions).toHaveBeenCalledWith(1, [1]);
    expect(component.selectedQuestions.size).toBe(0);
    expect(component.view).toBe('questions'); // stays for continued editing
    expect(component.selectedExam.questions.length).toBe(1); // refreshed detail
  });

  it('ignores a second attach click while a request is in flight', () => {
    component.selectedExam = draftExam(20);
    component.toggleQuestionSelect(bankQuestion(1, 10));
    component.isSaving = true; // request already pending

    component.handleAttachClick();

    expect(examService.attachQuestions).not.toHaveBeenCalled();
  });

  // ─── Publish readiness ──────────────────────────────────────────────

  it('reports Incomplete until attached marks equal the configured total', () => {
    component.selectedExam = draftExam(20, [attachedQuestion(1, 12)]);
    expect(component.getReadiness()).toBe('Incomplete');
  });

  it('reports Ready when attached marks exactly match the total', () => {
    component.selectedExam = draftExam(20, [attachedQuestion(1, 12), attachedQuestion(2, 8)]);
    expect(component.getReadiness()).toBe('Ready');
  });

  it('reports Published for non-draft exams', () => {
    component.selectedExam = draftExam(20);
    component.selectedExam.examStatus = 'Published';
    expect(component.getReadiness()).toBe('Published');
  });

  // ─── Publish / unpublish guards ─────────────────────────────────────

  it('publishes after confirmation and blocks double-clicks', () => {
    component.handlePublishClick(1);
    expect(examService.publishExam).toHaveBeenCalledTimes(1);

    // While pending (isPublishing), a second click must be ignored.
    component.isPublishing = true;
    component.handlePublishClick(1);
    expect(examService.publishExam).toHaveBeenCalledTimes(1);
  });

  it('does not publish when the confirmation is declined', () => {
    vi.stubGlobal('confirm', vi.fn(() => false));
    component.handlePublishClick(1);
    expect(examService.publishExam).not.toHaveBeenCalled();
  });

  it('unpublish calls the service after confirmation', () => {
    component.handleUnpublishClick(1);
    expect(examService.unpublishExam).toHaveBeenCalledWith(1);
  });

  // ─── Category breakdown ─────────────────────────────────────────────

  it('builds a per-category breakdown of attached questions', () => {
    component.selectedExam = draftExam(30, [
      attachedQuestion(1, 10, 1),
      attachedQuestion(2, 10, 1),
      attachedQuestion(3, 10, 2),
    ]);

    const breakdown = component.getCategoryBreakdown();

    expect(breakdown).toContain('2 × C# & .NET');
    expect(breakdown).toContain('1 × DBMS');
  });
});
