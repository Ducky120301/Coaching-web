export default {
  paths: ['../features/**/*.feature'],
  import: ['e2e/support/**/*.ts', 'e2e/steps/**/*.ts'],
  tags: '@web and not @manual',
  format: ['progress', 'html:e2e/reports/report.html'],
};
