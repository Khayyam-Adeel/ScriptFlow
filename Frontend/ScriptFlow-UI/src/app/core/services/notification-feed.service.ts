import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PrescriptionStatus } from '../../shared/models/prescription-status';
import { PrescriptionHubService } from './prescription-hub.service';

export type FeedKind = 'status' | 'system';

export interface FeedItem {
  id: string;
  kind: FeedKind;
  /** Present for prescription status events, so the item can deep-link to the prescription. */
  prescriptionId?: string;
  status?: PrescriptionStatus;
  title: string;
  message: string;
  timestampMs: number;
  read: boolean;
}

const STORAGE_KEY = 'scriptflow.notifications';
const MAX_ITEMS = 100;

/**
 * A durable, session-spanning feed of every prescription-lifecycle event that arrives over the
 * SignalR hub (status transitions) plus system alerts (dead-lettered messages). Distinct from
 * NotificationService, which is the transient toast queue — this is the persistent record the
 * notification center renders, with unread tracking and localStorage persistence so it survives
 * a page reload. Cleared on logout (see AuthService) so one user's feed never leaks to the next.
 */
@Injectable({ providedIn: 'root' })
export class NotificationFeedService {
  private readonly hub = inject(PrescriptionHubService);

  readonly items = signal<FeedItem[]>(this.load());
  readonly unreadCount = computed(() => this.items().filter((item) => !item.read).length);

  constructor() {
    this.hub.statusChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(({ prescriptionId, status }) => this.addStatusEvent(prescriptionId, status));

    this.hub.messageDeadLettered$
      .pipe(takeUntilDestroyed())
      .subscribe(({ eventType }) =>
        this.add({
          kind: 'system',
          title: 'System alert',
          message: `${eventType} failed permanently and was moved to the dead-letter queue.`,
        }),
      );
  }

  markAllRead(): void {
    this.items.update((items) => items.map((item) => ({ ...item, read: true })));
    this.persist();
  }

  markRead(id: string): void {
    this.items.update((items) => items.map((item) => (item.id === id ? { ...item, read: true } : item)));
    this.persist();
  }

  remove(id: string): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
    this.persist();
  }

  clear(): void {
    this.items.set([]);
    this.persist();
  }

  private addStatusEvent(prescriptionId: string, status: PrescriptionStatus): void {
    this.add({
      kind: 'status',
      prescriptionId,
      status,
      title: STATUS_TITLES[status] ?? `Prescription ${status}`,
      message: STATUS_MESSAGES[status] ?? `A prescription is now ${status}.`,
    });
  }

  private add(partial: Omit<FeedItem, 'id' | 'timestampMs' | 'read'>): void {
    const item: FeedItem = {
      ...partial,
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      timestampMs: Date.now(),
      read: false,
    };
    this.items.update((items) => [item, ...items].slice(0, MAX_ITEMS));
    this.persist();
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.items()));
    } catch {
      // Storage full or unavailable (private mode) — the in-memory feed still works this session.
    }
  }

  private load(): FeedItem[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as FeedItem[]) : [];
    } catch {
      return [];
    }
  }
}

const STATUS_TITLES: Record<PrescriptionStatus, string> = {
  Created: 'Prescription created',
  Signed: 'Prescription signed',
  Dispatched: 'Sent to pharmacy',
  Acknowledged: 'Acknowledged by pharmacy',
  Rejected: 'Rejected by pharmacy',
  Expired: 'Prescription expired',
};

const STATUS_MESSAGES: Record<PrescriptionStatus, string> = {
  Created: 'A new prescription was created and is awaiting signature.',
  Signed: 'A prescription was signed and is on its way to the pharmacy.',
  Dispatched: 'A prescription was dispatched to the pharmacy for dispensing.',
  Acknowledged: 'The pharmacy acknowledged and accepted a prescription.',
  Rejected: 'The pharmacy rejected a prescription — review the reason and follow up.',
  Expired: 'A prescription expired before completing its lifecycle.',
};
