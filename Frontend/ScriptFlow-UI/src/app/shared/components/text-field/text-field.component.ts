import { Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

/**
 * Label + input + validation message, bound directly to a parent FormControl.
 * Always `width: 100%` of its grid cell (see text-field.component.css), so it can never
 * be wider than the .form container it lives in.
 */
@Component({
  selector: 'app-text-field',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './text-field.component.html',
  styleUrl: './text-field.component.css',
})
export class TextFieldComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) control!: FormControl;
  @Input() type: 'text' | 'email' | 'password' | 'number' | 'date' = 'text';
  @Input() placeholder = '';
  @Input() errorMessage = '';
  @Input() fullWidth = false;

  get showError(): boolean {
    return this.control.invalid && (this.control.dirty || this.control.touched);
  }
}
