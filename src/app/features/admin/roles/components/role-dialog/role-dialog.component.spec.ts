import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Role } from '../../../../../core/api/roles/models/role.model';
import { RolesFacade } from '../../services/roles-facade.service';
import { RoleDialogComponent } from './role-dialog.component';

describe('RoleDialogComponent', () => {
  let fixture: ComponentFixture<RoleDialogComponent>;
  let component: RoleDialogComponent;

  const facadeMock = {
    saving: signal(false),
    createRole: vi.fn(),
    updateRole: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoleDialogComponent],
      providers: [{ provide: RolesFacade, useValue: facadeMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(RoleDialogComponent);
    component = fixture.componentInstance;
    vi.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should keep the form invalid when the role name is empty', () => {
    component.form.setValue({ name: '' });

    expect(component.form.invalid).toBe(true);
    expect(component.form.controls.name.hasError('required')).toBe(true);
  });

  it('should reject a role name shorter than two characters', () => {
    component.form.setValue({ name: 'A' });

    expect(component.form.invalid).toBe(true);
    expect(component.form.controls.name.hasError('minlength')).toBe(true);
  });

  it('should accept a valid role name', () => {
    component.form.setValue({ name: 'Manager' });

    expect(component.form.valid).toBe(true);
  });

  it('should display the validation message for an invalid touched name', () => {
    fixture.componentRef.setInput('visible', true);
    component.form.controls.name.setValue('');
    component.form.controls.name.markAsTouched();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const errorMessage = element.querySelector('small');

    expect(errorMessage).toBeTruthy();
    expect(errorMessage?.textContent).toContain('Role name is required');
  });

  it('should populate the form when a role is provided', () => {
    const role: Role = { id: 'role-1', name: 'Administrator' };

    fixture.componentRef.setInput('role', role);

    expect(component.form.controls.name.value).toBe('Administrator');
  });

  it('should create a role when saving a valid new role', () => {
    component.form.setValue({ name: 'Manager' });

    component.save();

    expect(facadeMock.createRole).toHaveBeenCalledOnce();
    expect(facadeMock.createRole).toHaveBeenCalledWith('Manager');
    expect(facadeMock.updateRole).not.toHaveBeenCalled();
  });

  it('should update an existing role when saving', () => {
    const role: Role = { id: 'role-1', name: 'Old name' };
    fixture.componentRef.setInput('role', role);
    component.form.setValue({ name: 'New name' });

    component.save();

    expect(facadeMock.updateRole).toHaveBeenCalledOnce();
    expect(facadeMock.updateRole).toHaveBeenCalledWith('role-1', 'New name');
    expect(facadeMock.createRole).not.toHaveBeenCalled();
  });

  it('should not call the facade when the form is invalid', () => {
    component.form.setValue({ name: '' });

    component.save();

    expect(facadeMock.createRole).not.toHaveBeenCalled();
    expect(facadeMock.updateRole).not.toHaveBeenCalled();
  });

  it('should emit false and reset the form when closing', () => {
    const visibleChangeSpy = vi.fn();
    component.visible = true;
    component.form.setValue({ name: 'Manager' });
    component.visibleChange.subscribe(visibleChangeSpy);

    component.close();

    expect(component.visible).toBe(false);
    expect(visibleChangeSpy).toHaveBeenCalledWith(false);
    expect(component.form.controls.name.value).toBeNull();
  });
});
