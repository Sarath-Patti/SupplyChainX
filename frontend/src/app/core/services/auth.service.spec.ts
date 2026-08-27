import { TestBed } from '@angular/core';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AuthResponse, User } from '../models/auth.model';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const mockUser: User = {
    id: 'user-123',
    username: 'testuser',
    email: 'test@example.com',
    roles: ['Operator'],
    isActive: true,
    createdAtUtc: new Date().toISOString()
  };

  const mockAuthResponse: AuthResponse = {
    token: 'fake-jwt-token',
    user: mockUser
  };

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize as unauthenticated when no stored token exists', async () => {
    const initialized = await service.initializeSession();
    expect(initialized).toBeFalse();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.isInitialized()).toBeTrue();
  });

  it('should restore session from stored token via /auth/me', async () => {
    localStorage.setItem('supplychainx_token', 'valid-stored-token');

    const initPromise = service.initializeSession();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/v1/auth/me`);
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);

    const initialized = await initPromise;
    expect(initialized).toBeTrue();
    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getUser()?.username).toBe('testuser');
    expect(service.getUser()?.roles).toContain('Operator');
  });

  it('should clear session if /auth/me returns 401 during startup', async () => {
    localStorage.setItem('supplychainx_token', 'expired-token');

    const initPromise = service.initializeSession();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/v1/auth/me`);
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const initialized = await initPromise;
    expect(initialized).toBeFalse();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getToken()).toBeNull();
  });

  it('should authenticate user and store token/user on login', () => {
    service.login({ username: 'testuser', password: 'password' }).subscribe((res) => {
      expect(res.token).toBe('fake-jwt-token');
      expect(res.user.username).toBe('testuser');
      expect(service.isLoggedIn()).toBeTrue();
      expect(service.getUser()?.username).toBe('testuser');
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/v1/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockAuthResponse);
  });

  it('should clear stored session on logout', () => {
    service.login({ username: 'testuser', password: 'password' }).subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/v1/auth/login`);
    req.flush(mockAuthResponse);

    expect(service.isLoggedIn()).toBeTrue();

    service.logout();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getUser()).toBeNull();
    expect(service.getToken()).toBeNull();
  });

  it('should correctly evaluate role checks', () => {
    service.currentUser.set(mockUser);

    expect(service.hasRole(['Operator'])).toBeTrue();
    expect(service.hasRole(['Admin'])).toBeFalse();
    expect(service.isOperator()).toBeTrue();
    expect(service.isAdmin()).toBeFalse();
    expect(service.canWrite()).toBeTrue();
  });
});
