import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { CanComponentDeactivate } from './can-deactivate.interface';

export const dirtyCheckGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  if (component.canDeactivate ? component.canDeactivate() : true) {
    return true;
  }

  const confirmationService = inject(ConfirmationService);

  return new Promise<boolean>((resolve) => {
    confirmationService.confirm({
      message: 'Your modifications haven\'t been saved. Discard your changes?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Discard',
      rejectLabel: 'Continue Editing',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => resolve(true),
      reject: () => resolve(false)
    });
  });
};
