import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { DeadLetterQueueService } from '../../../core/services/dead-letter-queue.service';
import { NotificationService } from '../../../core/services/notification.service';
import { DeadLetterMessage, DeadLetterQueueSummary } from '../../../core/models/dead-letter-queue.model';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';

/** Friendlier label per known queue, for display only - the raw queue name (needed for the
 * API calls) is kept alongside it rather than replaced. */
const QUEUE_LABELS: Record<string, string> = {
  'dispatch.prescription-signed.dlq': 'Dispatch.Worker — prescription signed',
  'scriptflow-api.prescription-dispatched.dlq': 'ScriptFlow.API — prescription dispatched',
  'scriptflow-api.prescription-acknowledged.dlq': 'ScriptFlow.API — prescription acknowledged',
  'scriptflow-api.prescription-rejected.dlq': 'ScriptFlow.API — prescription rejected',
  'notification.prescription-status-changed.dlq': 'Notification.Service — status changed',
  'notification.message-dead-lettered.dlq': 'Notification.Service — system alerts',
  'notification.token-revoked.dlq': 'Notification.Service — token revoked',
};

/**
 * Admin visibility into every dead-letter queue this system declares - the "where did a dropped
 * prescription end up" view. A message landed here only after RabbitMqEventConsumer's own
 * retries were exhausted (see that class), so a non-zero count is always something worth an
 * admin's attention, not routine noise. Peeking a queue is non-destructive (see
 * IDlqRedriveService.PeekAsync) so browsing this page can never itself lose a message; only
 * Redrive mutates anything.
 */
@Component({
  selector: 'app-admin-dlq',
  standalone: true,
  imports: [RouterLink, DatePipe, ButtonComponent, SpinnerComponent, IconComponent],
  templateUrl: './admin-dlq.component.html',
  styleUrl: './admin-dlq.component.css',
})
export class AdminDlqComponent {
  private readonly dlqService = inject(DeadLetterQueueService);
  private readonly notifications = inject(NotificationService);

  readonly loading = signal(true);
  readonly queues = signal<DeadLetterQueueSummary[]>([]);
  readonly totalStuck = computed(() => this.queues().reduce((sum, q) => sum + q.messageCount, 0));

  readonly selectedQueue = signal<string | null>(null);
  readonly messagesLoading = signal(false);
  readonly messages = signal<DeadLetterMessage[]>([]);
  readonly redrivingQueue = signal<string | null>(null);
  readonly expandedPayload = signal<string | null>(null);

  constructor() {
    this.load();
  }

  queueLabel(queueName: string): string {
    return QUEUE_LABELS[queueName] ?? queueName;
  }

  load(): void {
    this.loading.set(true);
    this.dlqService.getSummary().subscribe((queues) => {
      this.queues.set(queues);
      this.loading.set(false);

      // Keep the drill-down panel in sync if its queue's count changed (e.g. after a redrive).
      const selected = this.selectedQueue();
      if (selected) {
        this.viewMessages(selected);
      }
    });
  }

  viewMessages(queueName: string): void {
    this.selectedQueue.set(queueName);
    this.expandedPayload.set(null);
    this.messagesLoading.set(true);
    this.dlqService
      .peekMessages(queueName)
      .pipe(finalize(() => this.messagesLoading.set(false)))
      .subscribe((messages) => this.messages.set(messages));
  }

  closeMessages(): void {
    this.selectedQueue.set(null);
    this.messages.set([]);
  }

  togglePayload(messageId: string | null, index: number): void {
    const key = messageId ?? `#${index}`;
    this.expandedPayload.set(this.expandedPayload() === key ? null : key);
  }

  isPayloadExpanded(messageId: string | null, index: number): boolean {
    const key = messageId ?? `#${index}`;
    return this.expandedPayload() === key;
  }

  redrive(queueName: string): void {
    const count = this.queues().find((q) => q.queueName === queueName)?.messageCount ?? 0;
    if (
      !confirm(
        `Redrive ${count} message${count === 1 ? '' : 's'} from ${this.queueLabel(queueName)}? ` +
          'Each one gets republished for its original consumer to reprocess.',
      )
    ) {
      return;
    }

    this.redrivingQueue.set(queueName);
    this.dlqService
      .redrive(queueName)
      .pipe(finalize(() => this.redrivingQueue.set(null)))
      .subscribe((result) => {
        this.notifications.success(`Redrove ${result.redrivenCount} message(s) from ${this.queueLabel(queueName)}.`);
        this.load();
      });
  }
}
