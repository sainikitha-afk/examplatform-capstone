import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-performance-chart-modal',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './performance-chart-modal.html',
  styleUrl: './performance-chart-modal.css'
})
export class PerformanceChartModal implements OnChanges, OnInit, OnDestroy {
  @Input() topicScoresUrl: string | null = null;
  @Output() close = new EventEmitter<void>();

  isLoading: boolean = false;
  errorMessage: string = '';

  radarChartData: ChartConfiguration<'radar'>['data'] | null = null;
  radarChartOptions: ChartConfiguration<'radar'>['options'] = {
    responsive: true,
    onHover: (event, chartElement) => {
      if (event.native && event.native.target) {
        const target = event.native.target as HTMLElement;
        target.style.cursor = chartElement?.length ? 'pointer' : 'default';
      }
    },
    scales: {
      r: {
        suggestedMin: 0,
        suggestedMax: 100,
        ticks: { display: false },
        pointLabels: { font: { weight: 'bold', size: 12 } }
      }
    },
    interaction: {
      mode: 'point',
      intersect: true
    },
    plugins: {
      legend: { display: false },
      title: { display: false },
      tooltip: {
        callbacks: {
          title: (tooltipItems: any) => {
            return 'Subject Score';
          },
          label: (context: any) => {
            const value = context.raw as number;
            let labelName = context.chart.data.labels[context.dataIndex];
            if (Array.isArray(labelName)) labelName = labelName.join(' ');
            return `${labelName}: ${value}%`;
          }
        }
      }
    }
  };

  topicScores: { topic: string, score: number }[] = [];

  constructor(private http: HttpClient, private el: ElementRef) {}

  ngOnInit(): void {
    // Move the component to the body to escape stacking contexts (z-index/transform traps)
    document.body.appendChild(this.el.nativeElement);
  }

  ngOnDestroy(): void {
    this.el.nativeElement.remove();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['topicScoresUrl'] && this.topicScoresUrl) {
      this.loadTopicScores(this.topicScoresUrl);
    }
  }

  /**
   * Chart.js defaults its tick, point-label and grid colours to a fixed dark grey, which is
   * invisible against the dark-mode card. Read the active theme (data-theme on <html>, set by
   * the navigation toggle) and colour the chart to match. Purely presentational — the data,
   * the scores and the request are untouched.
   */
  private applyThemeColors(): void {
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';

    const labelColor = isDark ? '#f1f5f9' : '#0f172a';   // --text
    const lineColor = isDark ? 'rgba(241, 245, 249, 0.18)' : 'rgba(15, 23, 42, 0.12)';

    const scale = this.radarChartOptions?.scales?.['r'] as any;
    if (!scale) return;

    scale.pointLabels = { ...scale.pointLabels, color: labelColor };
    scale.ticks = { ...scale.ticks, color: labelColor };
    scale.grid = { ...scale.grid, color: lineColor };
    scale.angleLines = { ...scale.angleLines, color: lineColor };
  }

  private loadTopicScores(url: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.applyThemeColors();

    const fullUrl = url.startsWith('/') ? `http://localhost:5014${url}` : url;

    this.http.get<Record<string, number>>(fullUrl).subscribe({
      next: (scores) => {
        this.topicScores = Object.entries(scores).map(([topic, score]) => ({ topic, score }));
        
        const labels = Object.keys(scores).map(k => {
          if (k.length <= 12 || !k.includes(' ')) return k;
          const words = k.split(' ');
          const mid = Math.ceil(words.length / 2);
          return [words.slice(0, mid).join(' '), words.slice(mid).join(' ')] as any;
        });
        const data = Object.values(scores);

        this.radarChartData = {
          labels: labels,
          datasets: [
            {
              label: 'Topic Score (%)',
              data: data,
              backgroundColor: 'rgba(16, 185, 129, 0.2)', // primary teal, transparent
              borderColor: '#10b981',
              pointBackgroundColor: '#10b981',
              pointBorderColor: '#fff',
              pointHoverBackgroundColor: '#fff',
              pointHoverBorderColor: '#10b981'
            }
          ]
        };
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load chart data.';
        this.isLoading = false;
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }
}
