import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./layouts/app-shell/app-shell.component').then((m) => m.AppShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'patients',
        loadComponent: () =>
          import('./features/patients/patient-search/patient-search.component').then(
            (m) => m.PatientSearchComponent,
          ),
      },
      {
        path: 'patients/new',
        loadComponent: () =>
          import('./features/patients/patient-form/patient-form.component').then(
            (m) => m.PatientFormComponent,
          ),
      },
      {
        path: 'patients/:id',
        loadComponent: () =>
          import('./features/patients/patient-detail/patient-detail.component').then(
            (m) => m.PatientDetailComponent,
          ),
      },
      {
        path: 'providers',
        loadComponent: () =>
          import('./features/providers/provider-list/provider-list.component').then(
            (m) => m.ProviderListComponent,
          ),
      },
      {
        path: 'providers/new',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/providers/provider-form/provider-form.component').then(
            (m) => m.ProviderFormComponent,
          ),
      },
      {
        path: 'medicines',
        loadComponent: () =>
          import('./features/medicines/medicine-list/medicine-list.component').then(
            (m) => m.MedicineListComponent,
          ),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/notification-center/notification-center.component').then(
            (m) => m.NotificationCenterComponent,
          ),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/admin/admin-overview/admin-overview.component').then(
            (m) => m.AdminOverviewComponent,
          ),
      },
      {
        path: 'admin/practices',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/admin/admin-practices/admin-practices.component').then(
            (m) => m.AdminPracticesComponent,
          ),
      },
      {
        path: 'admin/register-admin',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/admin/register-admin-user/register-admin-user.component').then(
            (m) => m.RegisterAdminUserComponent,
          ),
      },
      {
        path: 'admin/dlq',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/admin/admin-dlq/admin-dlq.component').then((m) => m.AdminDlqComponent),
      },
      {
        path: 'providers/:id',
        loadComponent: () =>
          import('./features/providers/provider-detail/provider-detail.component').then(
            (m) => m.ProviderDetailComponent,
          ),
      },
      {
        path: 'prescriptions',
        loadComponent: () =>
          import('./features/prescriptions/prescription-list/prescription-list.component').then(
            (m) => m.PrescriptionListComponent,
          ),
      },
      {
        path: 'prescriptions/new',
        loadComponent: () =>
          import('./features/prescriptions/prescription-form/prescription-form.component').then(
            (m) => m.PrescriptionFormComponent,
          ),
      },
      {
        path: 'prescriptions/:id/edit',
        loadComponent: () =>
          import('./features/prescriptions/prescription-form/prescription-form.component').then(
            (m) => m.PrescriptionFormComponent,
          ),
      },
      {
        path: 'prescriptions/:id',
        loadComponent: () =>
          import('./features/prescriptions/prescription-detail/prescription-detail.component').then(
            (m) => m.PrescriptionDetailComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
