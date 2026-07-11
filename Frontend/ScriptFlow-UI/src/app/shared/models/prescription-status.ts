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
