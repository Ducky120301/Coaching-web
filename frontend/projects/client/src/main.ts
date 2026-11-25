import { bootstrapApplication } from '@angular/platform-browser';
import { APP_CONFIG, loadAppConfig } from '@coaching/shared';
import { appConfig } from './app/app.config';
import { App } from './app/app';

loadAppConfig()
  .then((config) =>
    bootstrapApplication(App, {
      ...appConfig,
      providers: [...appConfig.providers, { provide: APP_CONFIG, useValue: config }],
    }),
  )
  .catch((err) => console.error(err));
