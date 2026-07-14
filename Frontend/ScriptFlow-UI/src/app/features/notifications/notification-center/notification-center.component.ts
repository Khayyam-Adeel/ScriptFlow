import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FeedItem, NotificationFeedService } from '../../../core/services/notification-feed.service';
import { PrescriptionStatus } from '../../../shared/models/prescription-status';
import { IconComponent, IconName } from '../../../shared/components/icon/icon.component';

interface FeedGroup {
  label: string;
  items: FeedItem[];
}

/** Visual treatment per feed item — an icon and a colour tint keyed off the status/kind. */
interface FeedVisual {
  icon: IconName;
  tint: string;
}

/**
 * The notification center: a dashboard-style, always-available record of every prescription
 * lifecycle event (and system alert) captured by NotificationFeedService, grouped by day with
 * unread tracking. Purely a view over that service's signal — no fetching of its own.
 */
@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet, IconComponent],
  templateUrl: './notification-center.component.html',
  styleUrl: './notification-center.component.css',
})
export class NotificationCenterComponent {
  private readonly feed = inject(NotificationFeedService);

  readonly items = this.feed.items;
  readonly unreadCount = this.feed.unreadCount;
  readonly totalCount = computed(() => this.items().length);
  readonly rejectedCount = computed(
    () => this.items().filter((item) => item.status === 'Rejected').length,
  );

  /** Items bucketed into Today / Yesterday / Earlier for a calm, scannable layout. */
  readonly groups = computed<FeedGroup[]>(() => {
    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);
    const startOfYesterday = startOfToday.getTime() - 86_400_000;

    const buckets: Record<string, FeedItem[]> = { Today: [], Yesterday: [], Earlier: [] };
    for (const item of this.items()) {
      if (item.timestampMs >= startOfToday.getTime()) {
        buckets['Today'].push(item);
      } else if (item.timestampMs >= startOfYesterday) {
        buckets['Yesterday'].push(item);
      } else {
        buckets['Earlier'].push(item);
      }
    }

    return Object.entries(buckets)
      .filter(([, items]) => items.length > 0)
      .map(([label, items]) => ({ label, items }));
  });

  visual(item: FeedItem): FeedVisual {
    if (item.kind === 'system') {
      return { icon: 'alert', tint: 'red' };
    }
    return STATUS_VISUALS[item.status ?? 'Created'];
  }

  relativeTime(timestampMs: number): string {
    const seconds = Math.floor((Date.now() - timestampMs) / 1000);
    if (seconds < 60) {
      return 'just now';
    }
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return `${minutes}m ago`;
    }
    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return `${hours}h ago`;
    }
    const days = Math.floor(hours / 24);
    return days === 1 ? 'yesterday' : `${days}d ago`;
  }

  shortId(prescriptionId: string): string {
    return prescriptionId.slice(0, 8);
  }

  markAllRead(): void {
    this.feed.markAllRead();
  }

  clear(): void {
    this.feed.clear();
  }

  onItemClick(item: FeedItem): void {
    if (!item.read) {
      this.feed.markRead(item.id);
    }
  }

  remove(event: Event, id: string): void {
    event.stopPropagation();
    this.feed.remove(id);
  }
}

const STATUS_VISUALS: Record<PrescriptionStatus, FeedVisual> = {
  Created: { icon: 'file-text', tint: 'slate' },
  Signed: { icon: 'pen', tint: 'blue' },
  Dispatched: { icon: 'send', tint: 'amber' },
  Acknowledged: { icon: 'check-circle', tint: 'green' },
  Rejected: { icon: 'alert', tint: 'red' },
  Expired: { icon: 'clock', tint: 'slate' },
};
