import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { PurchaseOrderStepperComponent } from './purchase-order-stepper.component';

describe('PurchaseOrderStepperComponent', () => {
  let fixture: ComponentFixture<PurchaseOrderStepperComponent>;
  let component: PurchaseOrderStepperComponent;

  const messageServiceMock = {
    add: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseOrderStepperComponent],
      providers: [{ provide: MessageService, useValue: messageServiceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseOrderStepperComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('currentStep', 1);
    fixture.componentRef.setInput('canGoToMapping', true);
    fixture.componentRef.setInput('canGoToConfirmation', false);
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit the mapping step when it is available', () => {
    const stepChangeSpy = vi.fn();
    component.stepChange.subscribe(stepChangeSpy);

    component.onStepClick(2);

    expect(stepChangeSpy).toHaveBeenCalledWith(2);
    expect(messageServiceMock.add).not.toHaveBeenCalled();
  });

  it('should always allow returning to the upload step', () => {
    const stepChangeSpy = vi.fn();
    component.currentStep = 2;
    component.stepChange.subscribe(stepChangeSpy);

    component.onStepClick(1);

    expect(stepChangeSpy).toHaveBeenCalledWith(1);
  });

  it('should not emit when the current step is selected', () => {
    const stepChangeSpy = vi.fn();
    component.stepChange.subscribe(stepChangeSpy);

    component.onStepClick(1);

    expect(stepChangeSpy).not.toHaveBeenCalled();
    expect(messageServiceMock.add).not.toHaveBeenCalled();
  });

  it('should warn and return to the current step when a locked step is selected', () => {
    vi.useFakeTimers();
    const stepChangeSpy = vi.fn();
    component.stepChange.subscribe(stepChangeSpy);

    component.onStepClick(3);

    expect(messageServiceMock.add).toHaveBeenCalledWith({
      severity: 'warn',
      summary: 'Step locked',
      detail: 'Please complete previous steps first.',
    });
    expect(stepChangeSpy).not.toHaveBeenCalled();

    vi.runAllTimers();

    expect(stepChangeSpy).toHaveBeenCalledWith(1);
  });

  it('should emit the confirmation step when it is available', () => {
    const stepChangeSpy = vi.fn();
    component.canGoToConfirmation = true;
    component.stepChange.subscribe(stepChangeSpy);

    component.onStepClick(3);

    expect(stepChangeSpy).toHaveBeenCalledWith(3);
    expect(messageServiceMock.add).not.toHaveBeenCalled();
  });
});
