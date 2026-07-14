import { Component, Input } from '@angular/core';
import { code128ModuleWidths } from '../../utils/code128';

interface Bar {
  x: number;
  width: number;
}

/** Renders `value` as a real Code128 barcode (see `shared/utils/code128.ts`), with the
 * human-readable value underneath - matching how prescription print-outs show both. */
@Component({
  selector: 'app-barcode',
  standalone: true,
  template: `
    <svg
      [attr.width]="totalWidth"
      [attr.height]="svgHeight"
      [attr.viewBox]="'0 0 ' + totalWidth + ' ' + svgHeight"
      role="img"
      [attr.aria-label]="value"
    >
      @for (bar of bars; track bar.x) {
        <rect [attr.x]="bar.x" y="0" [attr.width]="bar.width" [attr.height]="height" fill="#000" />
      }
      @if (showText) {
        <text
          [attr.x]="totalWidth / 2"
          [attr.y]="height + 12"
          text-anchor="middle"
          font-family="monospace"
          font-size="12"
          fill="#000"
        >{{ value }}</text>
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-block;
      line-height: 0;
    }
  `,
})
export class BarcodeComponent {
  @Input({ required: true }) value = '';
  @Input() height = 50;
  @Input() showText = true;
  @Input() moduleWidth = 2;

  get bars(): Bar[] {
    const widths = code128ModuleWidths(this.value);
    let x = 0;
    const rects: Bar[] = [];

    widths.forEach((moduleCount, index) => {
      const width = moduleCount * this.moduleWidth;
      if (index % 2 === 0) {
        rects.push({ x, width });
      }
      x += width;
    });

    return rects;
  }

  get totalWidth(): number {
    const bars = this.bars;
    return bars.length ? bars[bars.length - 1].x + bars[bars.length - 1].width : 0;
  }

  get svgHeight(): number {
    return this.height + (this.showText ? 16 : 0);
  }
}
