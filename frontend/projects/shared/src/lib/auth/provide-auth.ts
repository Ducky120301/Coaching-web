import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { CLIENT_APP, ClientApp } from './client-app';

export function provideCoachingAuth(app: ClientApp): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: CLIENT_APP, useValue: app },
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => inject(AuthService).restore()),
  ]);
}
