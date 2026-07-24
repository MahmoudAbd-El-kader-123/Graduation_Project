# Frontend Testing Guide

This document explains the SPIP frontend unit-test setup, the behavior currently
covered, and how to run and extend the test suite.

## Test stack

The project uses:

- Angular 22 testing utilities and `TestBed`
- Vitest as the test runner
- jsdom as the simulated browser environment
- Angular's `@angular/build:unit-test` builder

Test files use the `.spec.ts` suffix and are discovered automatically by the
Angular test builder.

## Current test coverage

At the time this guide was written, the suite contains 48 tests in 4 test files.

### Application shell

File: `src/app/app.spec.ts`

This suite contains 2 tests:

- Creates the root `App` component successfully.
- Verifies that the application shell renders the Sonner toaster, PrimeNG
  toast, and Angular router outlet.

### All-component smoke tests

File: `src/app/components.spec.ts`

This suite contains one smoke test for each of the 30 component classes in the
project. It imports every component and verifies that the class is defined.

These tests catch problems such as:

- A component file that cannot be imported.
- TypeScript or Angular compilation errors in an imported component.
- A renamed, moved, or missing component export.

These are compilation smoke tests. They do **not** verify each component's
rendered HTML, user interaction, service calls, or business rules.

### Role dialog behavior

File:
`src/app/features/admin/roles/components/role-dialog/role-dialog.component.spec.ts`

This suite contains 10 tests covering:

- Component creation.
- Required validation for an empty role name.
- Minimum-length validation for a one-character role name.
- Acceptance of a valid role name.
- Display of the validation message after the field is touched.
- Populating the form from the `role` input.
- Calling `RolesFacade.createRole` for a valid new role.
- Calling `RolesFacade.updateRole` for an existing role.
- Preventing facade calls while the form is invalid.
- Closing the dialog, resetting the form, and emitting `visibleChange` with
  `false`.

`RolesFacade` is replaced with a mock. The tests therefore verify the
component/facade contract without making HTTP requests.

### Purchase-order stepper behavior

File:
`src/app/features/purchase-orders/import/components/purchase-order-stepper/purchase-order-stepper.component.spec.ts`

This suite contains 6 tests covering:

- Component creation.
- Moving to the mapping step when it is available.
- Always allowing navigation back to the upload step.
- Ignoring a click on the currently selected step.
- Blocking a locked step, displaying a warning, and restoring the current
  step.
- Moving to the confirmation step when it is available.

`MessageService` is replaced with a mock, allowing the warning message to be
verified without displaying a real toast.

## Test configuration

The test target is configured in `angular.json`:

```json
{
  "test": {
    "builder": "@angular/build:unit-test",
    "options": {
      "buildTarget": "SPIP.Frontend:build:test",
      "setupFiles": ["src/test-setup.ts"]
    }
  }
}
```

The dedicated `test` build configuration does not use local development or
production environment-file replacements. This allows tests to run on a fresh
clone without requiring ignored environment files.

`tsconfig.spec.json` supplies the Vitest global types, including `describe`,
`it`, `expect`, and `vi`.

### Browser API setup

The tests run in jsdom rather than a full browser. jsdom does not implement
every browser API.

`src/test-setup.ts` provides a `window.matchMedia` test implementation required
by the theme and notification libraries. The setup file runs before every
test file.

## Installing dependencies

Open a terminal in the Angular project directory:

```powershell
cd "D:\ITI-Professional Web Development\Gradaution Project\Graduation_Project-Sprint_two\frontend\SPIP.Frontend"
```

Install the locked dependencies:

```powershell
npm.cmd ci --legacy-peer-deps
```

`--legacy-peer-deps` is currently required because `lucide-angular@1.0.0`
declares Angular peer support through version 21, while this project uses
Angular 22.

On shells where `npm` works normally, `npm` can be used instead of `npm.cmd`.
Using `npm.cmd` avoids the PowerShell execution-policy error that can block
`npm.ps1` on Windows.

## Running tests

All commands below must be run from the `SPIP.Frontend` directory.

### Run once

Use this command locally and in continuous integration:

```powershell
npm.cmd test -- --watch=false
```

A successful run currently ends with output similar to:

```text
Test Files  4 passed (4)
Tests       48 passed (48)
```

The process exits with code `0` when all tests pass and a non-zero code when
the build or a test fails.

### Watch mode

During development, run:

```powershell
npm.cmd test
```

Vitest stays active and reruns affected tests when source or spec files change.
Stop watch mode with `Ctrl+C`.

### Run one spec file

Pass the spec path through Angular's `include` option:

```powershell
npm.cmd test -- --watch=false --include "src/app/features/admin/roles/components/role-dialog/role-dialog.component.spec.ts"
```

For the stepper:

```powershell
npm.cmd test -- --watch=false --include "src/app/features/purchase-orders/import/components/purchase-order-stepper/purchase-order-stepper.component.spec.ts"
```

### Run from the parent `frontend` directory

If the terminal is in the parent directory, use npm's prefix option:

```powershell
npm.cmd --prefix .\SPIP.Frontend test -- --watch=false
```

Running `npm.cmd test` directly from the parent directory fails with `ENOENT`
because that directory does not contain `package.json`.

## Understanding a test

A typical component test has three stages:

1. **Arrange:** create the component and configure inputs or service mocks.
2. **Act:** call a method, change a form value, or simulate an interaction.
3. **Assert:** verify the rendered result, emitted event, or service call.

Example:

```typescript
it('should create a role when saving a valid new role', () => {
  component.form.setValue({ name: 'Manager' });

  component.save();

  expect(facadeMock.createRole).toHaveBeenCalledWith('Manager');
});
```

Vitest mocks are created with `vi.fn()`:

```typescript
const facadeMock = {
  saving: signal(false),
  createRole: vi.fn(),
  updateRole: vi.fn(),
};
```

The mock is supplied through Angular dependency injection:

```typescript
providers: [{ provide: RolesFacade, useValue: facadeMock }]
```

This keeps unit tests fast and prevents real API calls.

## Adding a component behavior test

Create a `.spec.ts` file beside the component:

```text
example.component.ts
example.component.spec.ts
```

Basic standalone-component setup:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExampleComponent } from './example.component';

describe('ExampleComponent', () => {
  let fixture: ComponentFixture<ExampleComponent>;
  let component: ExampleComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExampleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ExampleComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

For required signal inputs, set them before the first change-detection run:

```typescript
fixture.componentRef.setInput('title', 'Total users');
fixture.componentRef.setInput('value', 150);
fixture.detectChanges();
```

For an output, subscribe and use a Vitest spy:

```typescript
const outputSpy = vi.fn();
component.valueChange.subscribe(outputSpy);

component.selectValue(2);

expect(outputSpy).toHaveBeenCalledWith(2);
```

For rendered HTML, update the state and run change detection:

```typescript
component.form.controls.name.markAsTouched();
fixture.detectChanges();

const element = fixture.nativeElement as HTMLElement;
expect(element.querySelector('small')).toBeTruthy();
```

## What to test next

The 30-component smoke suite gives broad compilation coverage, but most
components do not yet have focused behavior tests. Prioritize behavior tests
for:

- User create/edit/list forms and validation.
- Role deletion success and error flows.
- Permission selection, saving, and rollback behavior.
- Excel upload validation and API errors.
- Column mapping rules and required mappings.
- Purchase-order confirmation success and failure.
- Dashboard loading, success, empty, and error states.
- Authorization-dependent controls and navigation.

For API-dependent components, mock the service or facade and cover at least:

- Successful response.
- Error response.
- Loading state.
- Empty response, where applicable.
- Correct request parameters.

## Troubleshooting

### `ENOENT: Could not read package.json`

The command is running from the wrong directory. Change to `SPIP.Frontend` or
use:

```powershell
npm.cmd --prefix .\SPIP.Frontend test -- --watch=false
```

### `npm.ps1 cannot be loaded`

PowerShell script execution is restricted. Use `npm.cmd` instead of `npm`.

### Angular/lucide peer-dependency conflict

Install with:

```powershell
npm.cmd ci --legacy-peer-deps
```

The long-term fix is to replace deprecated `lucide-angular` with the compatible
`@lucide/angular` package after verifying application icon imports.

### Missing browser APIs

If a dependency requires a browser API that jsdom does not implement, add the
smallest suitable mock to `src/test-setup.ts`. Do not make real network calls
or depend on a full browser in a unit test.

