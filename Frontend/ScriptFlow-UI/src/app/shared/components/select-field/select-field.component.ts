import { Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
}

/** Label + native <select> + validation message, bound directly to a parent FormControl. */
@Component({
  selector: 'app-select-field',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './select-field.component.html',
  styleUrl: './select-field.component.css',
})
export class SelectFieldComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) control!: FormControl;
  @Input({ required: true }) options: SelectOption[] = [];
  @Input() placeholder = 'Select…';
  @Input() errorMessage = '';
  @Input() fullWidth = false;

  get showError(): boolean {
    return this.control.invalid && (this.control.dirty || this.control.touched);
  }
}
