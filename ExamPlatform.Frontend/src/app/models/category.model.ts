// CategoryModel — frontend twin of the ExamCategory entity on the backend.
// Plain TypeScript class. No Angular decorators. No magic.
// Used to hold category data we receive from GET /api/categories.

export class CategoryModel {
    constructor(
        public catId: number = 0,
        public catName: string = "",
        public description: string = ""
    ) {}
}