// exam-result.ts — Shows the per-question breakdown after results are published.
// CONCEPTS DEMO: route parameter (:attemptId), *ngFor table, [ngClass] for pass/fail colouring.

import { Component, OnInit } from '@angular/core';
import { environment } from '../../environments/environment';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { AttemptApiService } from '../services/attemptapi.service';
import { AttemptResultModel } from '../models/attempt-result.model';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-exam-result',
  imports: [CommonModule, RouterModule, BaseChartDirective],
  templateUrl: './exam-result.html',
  styleUrl: './exam-result.css',
})
export class ExamResult implements OnInit {
  result: AttemptResultModel = new AttemptResultModel();
  isLoading: boolean = true;
  errorMessage: string = '';

  radarChartData: ChartConfiguration<'radar'>['data'] | null = null;
  radarChartOptions: ChartConfiguration<'radar'>['options'] = {
    responsive: true,
    scales: {
      r: {
        suggestedMin: 0,
        suggestedMax: 100,
        ticks: { display: false },
        pointLabels: { font: { weight: 'bold', size: 14 } }
      }
    },
    plugins: {
      legend: { position: 'bottom' },
      title: { display: true, text: 'Candidate Performance Analytics', font: { size: 16 } }
    }
  };

  constructor(
    private route: ActivatedRoute,
    private attemptService: AttemptApiService,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const attemptId = Number(this.route.snapshot.params['attemptId']);
    this.attemptService.loadAttemptResult(attemptId).subscribe({
      next: (data) => {
        this.result = data;
        this.isLoading = false;
        if (this.result.topicScoresUrl) {
          this.loadTopicScores(this.result.topicScoresUrl);
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Results are not available yet.';
        this.isLoading = false;
      }
    });
  }

  private loadTopicScores(url: string): void {
    // If it's a relative URL from backend, append the backend URL
    const fullUrl = url.startsWith('/') ? `${environment.apiBaseUrl}${url}` : url;
    this.http.get<Record<string, number>>(fullUrl).subscribe({
      next: (scores) => {
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
              data: data,
              label: 'Topic Score %',
              backgroundColor: 'rgba(54, 162, 235, 0.2)',
              borderColor: 'rgb(54, 162, 235)',
              pointBackgroundColor: 'rgb(54, 162, 235)',
              pointBorderColor: '#fff',
              pointHoverBackgroundColor: '#fff',
              pointHoverBorderColor: 'rgb(54, 162, 235)'
            }
          ]
        };
      },
      error: (err) => console.error('Failed to load topic scores JSON', err)
    });
  }
}
