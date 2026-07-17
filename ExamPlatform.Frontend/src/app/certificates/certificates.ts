// certificates.ts — Lists the candidate's earned certificates and renders a
// fancy, printable certificate document on download.
// CONCEPTS DEMO: date pipe on issuedAt, *ngFor, *ngIf for revoked status.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CertificateApiService } from '../services/certificateapi.service';
import { CandidateCertificateModel, CertificateDownloadModel } from '../models/certificate.model';

@Component({
  selector: 'app-certificates',
  imports: [CommonModule, RouterModule],
  templateUrl: './certificates.html',
  styleUrl: './certificates.css',
})
export class Certificates implements OnInit {
  certs: CandidateCertificateModel[] = [];
  isLoading: boolean = true;
  errorMessage: string = '';
  downloadingId: number | null = null;

  constructor(private certService: CertificateApiService) {}

  ngOnInit(): void {
    this.certService.loadMyCertificates().subscribe({
      next: (data) => { this.certs = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Could not load certificates.'; this.isLoading = false; }
    });
  }

  handleDownloadClick(certId: number): void {
    this.downloadingId = certId;
    this.certService.downloadCertificate(certId).subscribe({
      next: (data) => {
        this.downloadingId = null;
        this.openPrintableCertificate(data);
      },
      error: () => { this.downloadingId = null; alert('Download failed.'); }
    });
  }

  // Builds a self-contained, print-ready certificate document and opens it in a
  // new tab. Premium certificates get the institute-branded gold design; standard
  // ones get the clean teal design. The new window's own Print dialog lets the
  // candidate save it as a PDF — no server-side PDF dependency needed.
  private openPrintableCertificate(c: CertificateDownloadModel): void {
    const win = window.open('', '_blank');
    if (!win) { alert('Please allow pop-ups to download your certificate.'); return; }
    win.document.open();
    win.document.write(this.buildCertificateHtml(c));
    win.document.close();
  }

  // HTML-escape any user-influenced text before injecting it into the document.
  private esc(v: string | null | undefined): string {
    return (v ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;')
      .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  private buildCertificateHtml(c: CertificateDownloadModel): string {
    const premium = c.isPremium;
    const name = this.esc(c.candidateName);
    const exam = this.esc(c.examTitle);
    const code = this.esc(c.certificateCode).toUpperCase();
    const institute = this.esc(c.instituteName);
    const logo = c.instituteLogoUrl ? this.esc(c.instituteLogoUrl) : '';
    const issued = new Date(c.issuedAt).toLocaleDateString(undefined,
      { year: 'numeric', month: 'long', day: 'numeric' });
    const pct = c.totalMarks > 0 ? Math.round((c.score / c.totalMarks) * 100) : 0;

    // Palette differs between premium (gold/navy) and standard (teal).
    const ink = premium ? '#B88A2E' : '#0D9488';
    const inkDeep = premium ? '#8A6416' : '#0F766E';
    const panelBg = premium ? '#FFFDF7' : '#FFFFFF';
    const pageBg = premium ? '#0F172A' : '#F1F5F9';
    const seal = premium ? '#C89B3C' : '#0D9488';

    const instituteBlock = (institute || logo) ? `
      <div class="institute">
        ${logo ? `<img src="${logo}" alt="Institute" class="inst-logo" onerror="this.style.display='none'"/>` : ''}
        ${institute ? `<div class="inst-name">${institute}</div>` : ''}
        <div class="inst-tag">Institute Verified${premium ? ' &middot; Premium Certification' : ''}</div>
      </div>` : `
      <div class="institute"><div class="inst-tag">ExamHub Verified Credential</div></div>`;

    return `<!doctype html><html><head><meta charset="utf-8"/>
<title>Certificate ${code}</title>
<style>
  @page { size: A4 landscape; margin: 0; }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: "Helvetica Neue", Arial, sans-serif;
         background: ${pageBg}; display: flex; align-items: center;
         justify-content: center; min-height: 100vh; padding: 28px; }
  .cert { position: relative; width: 1000px; max-width: 100%; aspect-ratio: 1.414;
          background: ${panelBg}; border-radius: 14px; overflow: hidden;
          box-shadow: 0 24px 60px rgba(0,0,0,.28); }
  .frame { position: absolute; inset: 22px; border: 2px solid ${ink};
           border-radius: 8px; }
  .frame::after { content: ""; position: absolute; inset: 7px;
                  border: 1px solid ${ink}55; border-radius: 5px; }
  .corner { position: absolute; width: 46px; height: 46px; border: 3px solid ${ink}; }
  .c-tl { top: 30px; left: 30px; border-right: 0; border-bottom: 0; border-radius: 8px 0 0 0; }
  .c-tr { top: 30px; right: 30px; border-left: 0; border-bottom: 0; border-radius: 0 8px 0 0; }
  .c-bl { bottom: 30px; left: 30px; border-right: 0; border-top: 0; border-radius: 0 0 0 8px; }
  .c-br { bottom: 30px; right: 30px; border-left: 0; border-top: 0; border-radius: 0 0 8px 0; }
  .body { position: relative; height: 100%; display: flex; flex-direction: column;
          align-items: center; justify-content: center; text-align: center;
          padding: 70px 90px; }
  .brand { display: flex; align-items: center; gap: 10px; margin-bottom: 6px; }
  .brand .mark { width: 34px; height: 34px; border-radius: 9px;
                 background: ${ink}; color: #fff; display: flex; align-items: center;
                 justify-content: center; font-size: 18px; }
  .brand .bname { font-size: 20px; font-weight: 800; letter-spacing: .5px; color: #0F172A; }
  .kicker { margin-top: 10px; text-transform: uppercase; letter-spacing: 4px;
            font-size: 13px; color: ${inkDeep}; font-weight: 700; }
  .title { font-family: Georgia, "Times New Roman", serif; font-size: 44px;
           color: #0F172A; margin: 6px 0 2px; }
  .subtitle { color: #64748B; font-size: 15px; margin-bottom: 18px; }
  .pname { font-family: Georgia, serif; font-size: 40px; font-weight: 700;
           color: ${inkDeep}; margin: 4px 0 6px; }
  .rule { width: 320px; height: 2px; background: ${ink}; margin: 6px auto 16px; opacity: .6; }
  .exam { font-size: 20px; color: #0F172A; font-weight: 600; margin-bottom: 4px; }
  .score { font-size: 15px; color: #475569; margin-bottom: 20px; }
  .score b { color: ${inkDeep}; }
  .institute { margin: 6px 0 8px; }
  .inst-logo { height: 46px; max-width: 160px; object-fit: contain; display: block; margin: 0 auto 6px; }
  .inst-name { font-size: 16px; font-weight: 700; color: #0F172A; }
  .inst-tag { font-size: 11px; letter-spacing: 1.5px; text-transform: uppercase;
              color: ${inkDeep}; font-weight: 700; }
  .foot { position: absolute; left: 90px; right: 90px; bottom: 62px;
          display: flex; justify-content: space-between; align-items: flex-end; }
  .sign { text-align: center; }
  .sign .line { width: 190px; border-top: 1.5px solid #94A3B8; margin-bottom: 4px; }
  .sign .lbl { font-size: 12px; color: #64748B; }
  .seal { width: 96px; height: 96px; border-radius: 50%;
          background: radial-gradient(circle at 35% 30%, ${seal}, ${inkDeep});
          color: #fff; display: flex; flex-direction: column; align-items: center;
          justify-content: center; box-shadow: 0 6px 18px ${ink}66;
          border: 4px solid ${panelBg}; outline: 2px solid ${ink}; }
  .seal .star { font-size: 26px; line-height: 1; }
  .seal .txt { font-size: 8.5px; letter-spacing: 1px; margin-top: 2px; font-weight: 700; }
  .code { position: absolute; bottom: 30px; left: 0; right: 0; text-align: center;
          font-size: 11px; letter-spacing: 2px; color: #94A3B8; }
  .actions { position: fixed; top: 14px; right: 14px; display: flex; gap: 8px; }
  .actions button { font: inherit; font-size: 13px; padding: 8px 14px; border-radius: 8px;
                    border: 0; cursor: pointer; color: #fff; background: ${inkDeep}; }
  .actions .ghost { background: #475569; }
  @media print { .actions { display: none; } body { background: #fff; padding: 0; }
                 .cert { box-shadow: none; border-radius: 0; } }
</style></head>
<body>
  <div class="actions">
    <button onclick="window.print()">⬇ Save as PDF / Print</button>
    <button class="ghost" onclick="window.close()">Close</button>
  </div>
  <div class="cert">
    <div class="frame"></div>
    <div class="corner c-tl"></div><div class="corner c-tr"></div>
    <div class="corner c-bl"></div><div class="corner c-br"></div>
    <div class="body">
      <div class="brand"><div class="mark">🎓</div><div class="bname">ExamHub</div></div>
      <div class="kicker">${premium ? 'Certificate of Premium Certification' : 'Certificate of Achievement'}</div>
      <div class="title">${premium ? 'Certified Achievement' : 'Certificate of Completion'}</div>
      <div class="subtitle">This is proudly presented to</div>
      <div class="pname">${name}</div>
      <div class="rule"></div>
      <div class="exam">for successfully completing &ldquo;${exam}&rdquo;</div>
      <div class="score">with a score of <b>${c.score}</b> / ${c.totalMarks} &nbsp;&middot;&nbsp; <b>${pct}%</b></div>
      ${instituteBlock}
      <div class="foot">
        <div class="sign"><div class="line"></div><div class="lbl">Issued ${this.esc(issued)}</div></div>
        <div class="seal"><div class="star">★</div><div class="txt">VERIFIED</div></div>
        <div class="sign"><div class="line"></div><div class="lbl">ExamHub Authority</div></div>
      </div>
    </div>
    <div class="code">CERTIFICATE ID &nbsp; ${code}</div>
  </div>
</body></html>`;
  }
}
