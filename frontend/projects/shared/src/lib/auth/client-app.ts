import { InjectionToken } from '@angular/core';

export type ClientApp = 'admin' | 'client';
export const CLIENT_APP = new InjectionToken<ClientApp>('CLIENT_APP');
