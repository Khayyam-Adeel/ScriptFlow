import { NotificationKind } from '../../core/services/notification.service';

// Mirrors Shared.contract.Enums.PrescriptionStatus. The API serializes enums as strings by default,
// so this must match the C# member names exactly.
export type PrescriptionStatus =
  | 'Created'
  | 'Signed'
  | 'Dispatched'
  | 'Acknowledged'
  | 'Rejected'
  | 'Expired';

export const PRESCRIPTION_STATUSES: PrescriptionStatus[] = [
  'Created',
  'Signed',
  'Dispatched',
  'Acknowledged',
  'Rejected',
  'Expired',
];

export function statusToastKind(status: PrescriptionStatus): NotificationKind {
  if (status === 'Acknowledged') {
    return 'success';
  }
  if (status === 'Rejected' || status === 'Expired') {
    return 'error';
  }
  return 'info';
}
