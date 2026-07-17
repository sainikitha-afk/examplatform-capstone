// category-board.ts — Reusable "category board" grid, shared by the candidate
// Browse Exams page and the Examiner category-browse page. Whenever a new
// category is added (via admin Manage Categories), it automatically shows up
// here as its own board — no per-consumer code needed.
//
// A parent passes in the categories (already loaded) and the currently
// selected id; clicking a board (or "All Categories") emits the new selection.
// Presentation-only: no HTTP calls live here.

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoryModel } from '../models/category.model';

// A small fixed palette of icon+color pairs, cycled deterministically by
// category id so the same category always renders with the same look across
// sessions/screens (no randomness, no stored "icon" column needed).
const BOARD_STYLES: { icon: string; accent: string }[] = [
  { icon: 'bi-code-slash',      accent: 'board-accent-teal' },
  { icon: 'bi-calculator',      accent: 'board-accent-amber' },
  { icon: 'bi-globe2',          accent: 'board-accent-blue' },
  { icon: 'bi-flask',           accent: 'board-accent-purple' },
  { icon: 'bi-cpu',             accent: 'board-accent-rose' },
  { icon: 'bi-book',            accent: 'board-accent-teal' },
  { icon: 'bi-graph-up',        accent: 'board-accent-amber' },
  { icon: 'bi-shield-lock',     accent: 'board-accent-blue' },
];

@Component({
  selector: 'app-category-board',
  imports: [CommonModule],
  templateUrl: './category-board.html',
  styleUrl: './category-board.css',
})
export class CategoryBoard {
  @Input() categories: CategoryModel[] = [];
  @Input() selectedCategoryId: number | null = null;
  // Label for the "all" board — callers customise per audience
  // ("All Categories" for candidates, "All Categories (my exams)" etc.).
  @Input() allLabel: string = 'All Categories';
  @Input() countLabel: string = 'exams';
  @Output() categorySelected = new EventEmitter<number | null>();

  select(catId: number | null): void {
    this.categorySelected.emit(catId);
  }

  styleFor(catId: number) {
    return BOARD_STYLES[catId % BOARD_STYLES.length];
  }

  totalCount(): number {
    return this.categories.reduce((sum, c) => sum + (c.publishedExamCount ?? 0), 0);
  }
}
