import { TestBed } from '@angular/core';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { roleGuard } from './role.guard';
import { AuthService } from '../services/auth.service';

describe('roleGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isInitialized', 'initializeSession', 'hasRole']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('should allow activation if user has required role after initialization', async () => {
    authServiceSpy.isInitialized.and.returnValue(true);
    authServiceSpy.hasRole.and.returnValue(true);

    const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => roleGuard(route, {} as any));

    expect(result).toBeTrue();
    expect(authServiceSpy.hasRole).toHaveBeenCalledWith(['Admin']);
  });

  it('should redirect to unauthorized if user lacks required role', async () => {
    authServiceSpy.isInitialized.and.returnValue(true);
    authServiceSpy.hasRole.and.returnValue(false);

    const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => roleGuard(route, {} as any));

    expect(result).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/unauthorized']);
  });
});
