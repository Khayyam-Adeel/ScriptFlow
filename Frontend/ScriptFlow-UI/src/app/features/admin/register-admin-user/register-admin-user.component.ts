import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { TextFieldComponent } from '../../../shared/components/text-field/text-field.component';

/** Admin-only: creates another Admin account (see AuthService.registerAdmin /
 * AuthController.RegisterAdmin). Stays on this page after a successful create - unlike
 * ProviderForm there's no detail page to navigate to, and an admin onboarding several
 * accounts in a row benefits from the form resetting in place. */
@Component({
  selector: 'app-register-admin-user',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, TextFieldComponent],
  templateUrl: './register-admin-user.component.html',
  styleUrl: './register-admin-user.component.css',
})
export class RegisterAdminUserComponent {
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly submitting = signal(false);

  // MinimumLength(8) mirrors RegisterAdminUserCommandValidator so the API never has to reject this.
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)],
    }),
  });

  get emailControl(): FormControl<string> {
    return this.form.controls.email;
  }

  get passwordControl(): FormControl<string> {
    return this.form.controls.password;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.authService
      .registerAdmin(email, password)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe((user) => {
        this.notifications.success(`Admin account created for ${user.email}.`);
        this.form.reset({ email: '', password: '' });
      });
  }
}
