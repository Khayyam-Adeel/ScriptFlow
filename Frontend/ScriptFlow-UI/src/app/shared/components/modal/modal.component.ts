import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { IconComponent } from '../icon/icon.component';

/** Generic overlay + panel, closable via the X button, a backdrop click, or Escape.
 * Content is projected in; callers own everything inside. */
@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css',
})
export class ModalComponent {
  @Input() title = '';
  @Output() closed = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }

  close(): void {
    this.closed.emit();
  }
}
