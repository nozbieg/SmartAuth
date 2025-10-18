import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../auth/FeatureFlagsContext', () => ({
  useFeatureFlags: () => ({ flags: { twofa_code: true }, loading: false })
}));

vi.mock('../../auth/AuthService', () => {
  const loginWithPassword = vi.fn();
  const verifyCode = vi.fn();
  const saveJwt = vi.fn();
  class ApiError extends Error { metadata?: Record<string,string>; constructor(message: string, metadata?: Record<string,string>) { super(message); this.metadata = metadata; } }
  return { loginWithPassword, verifyCode, saveJwt, ApiError };
});

vi.mock('../../components/layout/AuthLayout', () => ({ default: ({ children }: any) => <div data-testid="layout">{children}</div> }));
vi.mock('../../components/ui/Button', () => ({ default: (props: any) => <button {...props} /> }));

import * as AuthService from '../../auth/AuthService';
import LoginPage from './LoginPage';

function renderLogin() {
  return render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('LoginPage', () => {
  it('renderuje formularz logowania (krok cred)', () => {
    renderLogin();
    expect(screen.getByRole('heading', { name: /Logowanie/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Hasło/i)).toBeInTheDocument();
  });

  it('udane logowanie bez 2FA zapisuje token', async () => {
    (AuthService.loginWithPassword as any).mockResolvedValue({ requires2Fa: false, token: 'final-jwt' });
    renderLogin();
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'user@example.com' } });
    fireEvent.change(screen.getByLabelText(/Hasło/i), { target: { value: 'Secret123' } });
    fireEvent.click(screen.getByRole('button', { name: /Zaloguj się/i }));

    await waitFor(() => expect(AuthService.saveJwt).toHaveBeenCalledWith('final-jwt'));
  });

  it('przechodzi do kroku 2FA i pokazuje selektor metod', async () => {
    (AuthService.loginWithPassword as any).mockResolvedValue({ requires2Fa: true, token: 'temp-token', methods: ['code'] });
    renderLogin();
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'user2@example.com' } });
    fireEvent.change(screen.getByLabelText(/Hasło/i), { target: { value: 'Secret123' } });
    fireEvent.click(screen.getByRole('button', { name: /Zaloguj się/i }));

    await screen.findByRole('heading', { name: /Drugi krok/i });
    expect(screen.getByRole('tab', { name: /CODE/i })).toBeInTheDocument();
  });

  it('weryfikuje kod TOTP i zapisuje końcowy jwt', async () => {
    (AuthService.loginWithPassword as any).mockResolvedValue({ requires2Fa: true, token: 'temp-token', methods: ['totp'] });
    (AuthService.verifyCode as any).mockResolvedValue({ jwt: 'final-jwt-2' });

    renderLogin();
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'user3@example.com' } });
    fireEvent.change(screen.getByLabelText(/Hasło/i), { target: { value: 'Secret123' } });
    fireEvent.click(screen.getByRole('button', { name: /Zaloguj się/i }));

    await screen.findByRole('heading', { name: /Drugi krok/i });
    fireEvent.change(screen.getByLabelText(/Kod TOTP/i), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: /Potwierdź TOTP/i }));

    await waitFor(() => expect(AuthService.verifyCode).toHaveBeenCalledWith('temp-token', '123456'));
    expect(AuthService.saveJwt).toHaveBeenCalledWith('final-jwt-2');
  });

  it('pokazuje błąd API przy nieudanym logowaniu', async () => {
    (AuthService.loginWithPassword as any).mockRejectedValue(new (AuthService.ApiError as any)('Błąd logowania', { Email: 'Niepoprawny email' }));
    renderLogin();
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'bad@example.com' } });
    fireEvent.change(screen.getByLabelText(/Hasło/i), { target: { value: 'x' } });
    fireEvent.click(screen.getByRole('button', { name: /Zaloguj się/i }));

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    expect(screen.getByText(/Błąd logowania/i)).toBeInTheDocument();
    expect(screen.getByText(/Niepoprawny email/i)).toBeInTheDocument();
  });
});
