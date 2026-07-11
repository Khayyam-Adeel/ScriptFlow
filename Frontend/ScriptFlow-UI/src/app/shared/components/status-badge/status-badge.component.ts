import { Component, Input } from '@angular/core';
import { PrescriptionStatus } from '../../models/prescription-status';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.css',
  host: {
    '[class]': "'status-' + status.toLowerCase()",
  },
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: PrescriptionStatus;
}
