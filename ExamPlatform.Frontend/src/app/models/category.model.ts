// CategoryModel — frontend twin of the ExamCategory entity on the backend.
// Plain TypeScript class. No Angular decorators. No magic.
// Used to hold category data we receive from GET /api/categories.

export class CategoryModel {
    constructor(
        public catId: number = 0,
        public catName: string = "",
        public description: string = "",
        // Published-exam count for this category. Only populated by the
        // GET /api/categories list endpoint (0 elsewhere) — powers the
        // category "board" cards on Browse Exams / examiner category browse.
        public publishedExamCount: number = 0
    ) {}
}