// categoryapi.service.spec.ts — unit tests for CategoryApiService.
// HttpTestingController intercepts calls; we assert method, URL, and body.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CategoryApiService } from './categoryapi.service';
import { CategoryModel } from '../models/category.model';

describe('CategoryApiService', () => {
  let service: CategoryApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/categories';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CategoryApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadCategories GETs the full list', () => {
    let result: CategoryModel[] = [];
    service.loadCategories().subscribe(r => result = r);

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush([new CategoryModel(1, 'C#'), new CategoryModel(2, 'SQL')]);

    expect(result.length).toBe(2);
    expect(result[0].catName).toBe('C#');
  });

  it('loadCategoryById GETs /{id}', () => {
    service.loadCategoryById(7).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/7`);
    expect(req.request.method).toBe('GET');
    req.flush(new CategoryModel(7, 'Java'));
  });

  it('createCategory POSTs name and description', () => {
    service.createCategory('Networking', 'OSI layers').subscribe();
    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.catName).toBe('Networking');
    req.flush(new CategoryModel(3, 'Networking'));
  });

  it('updateCategory PUTs to /{id}', () => {
    service.updateCategory(3, 'Networks', 'updated').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.catName).toBe('Networks');
    req.flush(new CategoryModel(3, 'Networks'));
  });

  it('deleteCategory sends DELETE /{id}', () => {
    service.deleteCategory(3).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
