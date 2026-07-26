import { Component, inject, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { StepperModule } from 'primeng/stepper';
import { ButtonModule } from 'primeng/button';
import { LucideAngularModule, ArrowLeft, ArrowRight, Check } from 'lucide-angular';
import { PoImportStoreService } from '../../services/po-import-store.service';
import { UploadStepComponent } from '../../components/upload-step/upload-step.component';
import { MapColumnsStepComponent } from '../../components/map-columns-step/map-columns-step.component';
import { ConfirmImportStepComponent } from '../../components/confirm-import-step/confirm-import-step.component';
import { PurchaseOrderStepperComponent } from '../../components/purchase-order-stepper/purchase-order-stepper.component';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-po-import-page',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    ButtonModule,
    LucideAngularModule,
    UploadStepComponent,
    MapColumnsStepComponent,
    ConfirmImportStepComponent,
    PurchaseOrderStepperComponent,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <div class="p-4 sm:p-6 lg:p-8 flex flex-col gap-6 h-full min-h-0 min-w-0 relative">
      <p-toast></p-toast>
      <div>
        <h1 class="text-2xl font-bold text-surface-900 dark:text-surface-0">Import Purchase Order</h1>
        <p class="text-surface-500 mt-1">Upload an Excel file to create a purchase order.</p>
      </div>

      <p-card styleClass="h-full flex flex-col min-h-0 min-w-0" class="flex-1 flex flex-col min-h-0 min-w-0">
        <div class="flex flex-col h-full overflow-hidden">
          <app-purchase-order-stepper
            [currentStep]="activeStep()"
            [canGoToMapping]="store.canProceed()"
            [canGoToConfirmation]="store.canGoToConfirmation()"
            (stepChange)="onStepChange($event)">
          </app-purchase-order-stepper>

          <div class="flex-1 min-h-0 overflow-hidden mt-6">
            @switch (activeStep()) {
              @case (1) {
                <div class="h-full flex flex-col min-h-0 min-w-0 overflow-y-auto">
                  <app-upload-step></app-upload-step>
                </div>
                <div class="flex pt-6 justify-end mt-4 shrink-0">
                  <p-button 
                    label="Next" 
                    icon="pi pi-arrow-right" 
                    iconPos="right" 
                    (onClick)="onNextClicked()">
                  </p-button>
                </div>
              }

              @case (2) {
                <div class="h-full flex flex-col min-h-0 min-w-0 overflow-y-auto">
                  <app-map-columns-step></app-map-columns-step>
                </div>
                <div class="flex pt-6 justify-between mt-4 shrink-0">
                  <p-button 
                    label="Back" 
                    severity="secondary" 
                    icon="pi pi-arrow-left" 
                    (onClick)="onStepChange(1)">
                  </p-button>
                  <p-button 
                    label="Next" 
                    icon="pi pi-arrow-right" 
                    iconPos="right" 
                    (onClick)="onStepChange(3)" 
                    [disabled]="!store.canGoToConfirmation()">
                  </p-button>
                </div>
              }

              @case (3) {
                <div class="h-full flex flex-col min-h-0 min-w-0 overflow-y-auto">
                  <app-confirm-import-step></app-confirm-import-step>
                </div>
                <div class="flex pt-6 justify-between mt-4 shrink-0">
                  <p-button 
                    label="Back" 
                    severity="secondary" 
                    icon="pi pi-arrow-left" 
                    (onClick)="onStepChange(2)">
                  </p-button>
                  <p-button 
                    label="Import Purchase Order" 
                    icon="pi pi-check" 
                    [loading]="store.savingMappings() || store.importing()"
                    (onClick)="onImportClicked()">
                  </p-button>
                </div>
              }
            }
          </div>
        </div>
      </p-card>
    </div>
  `
})
export class PoImportPageComponent implements OnDestroy {
  store = inject(PoImportStoreService);
  private messageService = inject(MessageService);

  activeStep = signal(1);

  ArrowLeftIcon = ArrowLeft;
  ArrowRightIcon = ArrowRight;
  CheckIcon = Check;

  onStepChange(step: number) {
    this.activeStep.set(step);
  }

  private getMissingRequirements(): string[] {
    const errors: string[] = [];

    if (!this.store.selectedVendor()) {
        errors.push('Select a vendor.');
    }

    if (!this.store.selectedFile()) {
        errors.push('Upload an Excel file.');
    }

    if (this.store.selectedHeaderRow() === null) {
        errors.push('Select a header row.');
    }

    return errors;
  }

  onNextClicked(): void {
    const errors = this.getMissingRequirements();

    if (errors.length > 0) {
        this.messageService.add({
            severity: 'warn',
            summary: 'Missing requirements',
            detail: errors.join('\n')
        });

        return;
    }

    this.onStepChange(2);
  }
  
  async onImportClicked() {
    try {
      await this.store.importPurchaseOrder();
    } catch (error) {
      // Error handled by store toasts
    }
  }

  ngOnDestroy() {
    this.store.reset();
  }
}
