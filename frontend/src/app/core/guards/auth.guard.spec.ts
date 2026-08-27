import { TestBed } from '@angular/core';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isInitialized', 'initializeSession', 'isLoggedIn']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('should await initializeSession when auth state is not yet initialized', async () => {
    authServiceSpy.isInitialized.and.returnValue(false);
    authServiceSpy.initializeSession.and.returnValue(Promise.resolve(true));
    authServiceSpy.isLoggedIn.and.returnValue(true);

    const dummyRoute = {} as ActivatedRouteSnapshot;
    const dummyState = { url: '/dashboard' } as RouterStateSnapshot;

    const result = await TestBed.runInInjectionContext(() => authGuard(dummyRoute, dummyState));

    expect(authServiceSpy.initializeSession).toHaveBeenCalled();
    expect(result).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('should allow navigation when user is logged in', async () => {
    authServiceSpy.isInitialized.and.returnValue(true);
    authServiceSpy.isLoggedIn.and.returnValue(true);

    const dummyRoute = {} as ActivatedRouteSnapshot;
    const dummyState = { url: '/dashboard' } as RouterStateSnapshot;

    const result = await TestBed.runInInjectionContext(() => authGuard(dummyRoute, dummyState));

    expect(result).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('should redirect to login when user is not logged in after initialization', async () => {
    authServiceSpy.isInitialized.and.returnValue(true);
    authServiceSpy.isLoggedIn.and.returnValue(false);

    const dummyRoute = {} as ActivatedRouteSnapshot;
    const dummyState = { url: '/products' } as RouterStateSnapshot;

    const result = await TestBed.runInInjectionContext(() => authGuard(dummyRoute, dummyState));

    expect(result).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/products' } });
  });
});
