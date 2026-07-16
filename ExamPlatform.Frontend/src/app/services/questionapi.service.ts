// QuestionApiService — HTTP calls for /api/questions endpoints.
// Why it exists: manage-questions component needs CRUD + Excel import in one place.
// How Angular uses it: constructor-injected; methods return Observables.

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { QuestionModel, CreateQuestionModel } from '../models/question.model';
import { PagedResultModel } from '../models/paged-result.model';

// ImportResultModel — response from POST /api/questions/import
interface ImportResult {
  successCount: number;
  failureCount: number;
  errors: string[];
  createdQuestionIds: number[];
}

@Injectable({ providedIn: 'root' })
export class QuestionApiService {
  private baseUrl = 'http://localhost:5014/api/questions';

  constructor(private http: HttpClient) {}

  // GET /api/questions?catId=&difficulty=&search=&page= (Examiner/Admin)
  // pageSize is configurable so the attach-questions screen can load the whole
  // bank (backend clamps to 100); list screens keep the default of 10.
  loadQuestions(catId: number | null = null, difficulty: string = '', search: string = '', page: number = 1, pageSize: number = 10): Observable<PagedResultModel<QuestionModel>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    // Only send optional filters when they carry a real value. We defensively
    // reject null/undefined/NaN and the string "null"/"undefined" (which a native
    // <select> can hand us) so we never emit e.g. ?categoryId=null → HTTP 400.
    const catNum = catId === null || catId === undefined ? NaN : Number(catId);
    if (!Number.isNaN(catNum) && catNum > 0) params = params.set('categoryId', catNum);
    if (difficulty && difficulty !== 'null' && difficulty !== 'undefined') params = params.set('difficulty', difficulty);
    if (search && search.trim()) params = params.set('search', search.trim());
    return this.http.get<PagedResultModel<QuestionModel>>(this.baseUrl, { params });
  }

  // GET /api/questions/{id} — single question with options (Examiner/Admin)
  loadQuestionById(id: number): Observable<QuestionModel> {
    return this.http.get<QuestionModel>(`${this.baseUrl}/${id}`);
  }

  // POST /api/questions (Examiner) — create a single question with options
  createQuestion(data: CreateQuestionModel): Observable<QuestionModel> {
    return this.http.post<QuestionModel>(this.baseUrl, data);
  }

  // PUT /api/questions/{id} (Examiner/Admin)
  updateQuestion(id: number, data: CreateQuestionModel): Observable<QuestionModel> {
    return this.http.put<QuestionModel>(`${this.baseUrl}/${id}`, data);
  }

  // DELETE /api/questions/{id} → 204 (soft-delete)
  deleteQuestion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // GET /api/questions/import-template — pre-formatted .xlsx the examiner fills in offline.
  // responseType 'blob' because the response is a binary file, not JSON.
  downloadImportTemplate(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/import-template`, { responseType: 'blob' });
  }

  // POST /api/questions/import — multipart/form-data Excel upload
  // The File is passed as a FormData object; Angular's HttpClient sets the Content-Type automatically.
  importExcel(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportResult>(`${this.baseUrl}/import`, formData);
  }
}
