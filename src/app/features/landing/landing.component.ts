import { Component, signal, HostListener } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {
  readonly year = new Date().getFullYear();
  readonly scrolled = signal(false);
  readonly mobileMenuOpen = signal(false);

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled.set(window.scrollY > 40);
  }

  readonly stats = [
    { value: '40%', label: 'Cost Reduction' },
    { value: '3×',  label: 'Faster Approvals' },
    { value: '99%', label: 'Invoice Match Rate' },
    { value: '500+', label: 'POs Processed / Mo' },
  ];

  readonly features = [
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>`,
      title: 'Smart Invoice Processing',
      desc: 'Automatically extract, validate, and route invoices with AI — eliminating manual data entry and human error.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>`,
      title: 'Purchase Order Automation',
      desc: 'Create, approve, and track purchase orders with configurable workflows and real-time status updates.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>`,
      title: '3-Way Reconciliation',
      desc: 'Match invoices, POs, and goods receipts automatically — catching discrepancies before payment.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>`,
      title: 'Real-Time Analytics',
      desc: 'Dashboards that give finance teams instant visibility into spend, vendor performance, and cash flow.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>`,
      title: 'Role-Based Access Control',
      desc: 'Granular permissions for admins, approvers, and vendors — keeping sensitive data secure at every level.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>`,
      title: 'AI Assistant',
      desc: 'Ask questions about your procurement data in plain language. Get instant answers, summaries, and insights.'
    },
  ];

  readonly steps = [
    { num: '01', title: 'Connect your vendors', desc: 'Onboard suppliers and configure your procurement catalogue in minutes.' },
    { num: '02', title: 'Automate your PO flow', desc: 'Set approval thresholds, assign roles, and let Fatorty route orders automatically.' },
    { num: '03', title: 'Match & pay with confidence', desc: 'Every invoice is reconciled against its PO — only clean invoices reach payment.' },
  ];
}
