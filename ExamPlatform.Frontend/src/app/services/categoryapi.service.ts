// CategoryApiService — all HTTP calls for the /api/categories endpoints.
// Why it exists: separates HTTP logic from the component; easy to swap the base URL in one place.
// How Angular uses it: @Injectable root singleton; components inject it via constructor.
// CONCEPT DEMO: map operator — transforms the raw JSON array into proper CategoryModel instances
//   so the rest of the app always works with typed objects, never raw plain objects.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CategoryModel } from '../models/category.model';

// Request body shapes for create/update.
interface CreateCategoryBody { catName: string; description: string; }
interface UpdateCategoryBody { catName: string; description: string; }

@Injectable({ providedIn: 'root' })
export class CategoryApiService {
  private baseUrl = 'http://localhost:5014/api/categories';

  constructor(private http: HttpClient) {}

  // GET /api/categories → array of CategoryModel
  // CONCEPT DEMO: map — converts each plain JSON object into a CategoryModel class instance.
  loadCategories(): Observable<CategoryModel[]> {
    return this.http.get<CategoryModel[]>(this.baseUrl).pipe(
      map(items => items.map(c => new CategoryModel(c.catId, c.catName, c.description)))
    );
  }

  // GET /api/categories/{id}
  loadCategoryById(id: number): Observable<CategoryModel> {
    return this.http.get<CategoryModel>(`${this.baseUrl}/${id}`).pipe(
      map(c => new CategoryModel(c.catId, c.catName, c.description))
    );
  }

  // POST /api/categories (Admin only — interceptor attaches token)
  createCategory(catName: string, description: string): Observable<CategoryModel> {
    const body: CreateCategoryBody = { catName, description };
    return this.http.post<CategoryModel>(this.baseUrl, body).pipe(
      map(c => new CategoryModel(c.catId, c.catName, c.description))
    );
  }

  // PUT /api/categories/{id} (Admin only)
  updateCategory(id: number, catName: string, description: string): Observable<CategoryModel> {
    const body: UpdateCategoryBody = { catName, description };
    return this.http.put<CategoryModel>(`${this.baseUrl}/${id}`, body).pipe(
      map(c => new CategoryModel(c.catId, c.catName, c.description))
    );
  }

  // DELETE /api/categories/{id} (Admin only) → 204 No Content
  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
