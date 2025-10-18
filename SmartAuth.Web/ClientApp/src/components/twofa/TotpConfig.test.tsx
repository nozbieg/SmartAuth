import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import TotpConfig from './TotpConfig';
import * as AuthService from '../../auth/AuthService';

vi.mock('../../auth/AuthService', () => {
  return {
    getJwt: () => localStorage.getItem('access_token'),
    totpStatus: vi.fn(),
    totpSetup: vi.fn(),
    totpEnable: vi.fn(),
    totpDisable: vi.fn(),
    ApiError: class ApiError extends Error {}
  };
});

vi.mock('../ui/Card', () => ({ default: (p: any) => { const { title, children } = p; return <div className="card">{title && <h2>{title}</h2>}{children}</div>; } }));
vi.mock('../ui/Button', () => ({ default: (p: any) => <button {...p}>{p.children}</button> }));

const totpStatusMock = AuthService.totpStatus as any;
const totpSetupMock = AuthService.totpSetup as any;
const totpEnableMock = AuthService.totpEnable as any;
const totpDisableMock = AuthService.totpDisable as any;

function setup(jwt: string | null) {
  if (jwt) localStorage.setItem('access_token', jwt); else localStorage.removeItem('access_token');
  return render(<TotpConfig />);
}

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

describe('TotpConfig', () => {
  it('zwraca null gdy brak jwt', () => {
    const { container } = setup(null);
    expect(container.firstChild).toBeNull();
  });
  it('pokazuje loading a potem stan nieaktywny', async () => {
    totpStatusMock.mockResolvedValue({ active: false });
    setup('jwt');
    expect(screen.getByText(/Ładowanie/i)).toBeInTheDocument();
    await waitFor(() => screen.getByText(/nie jest skonfigurowany/i));
  });
  it('rozpoczyna konfigurację i pokazuje secret i uri', async () => {
    totpStatusMock.mockResolvedValue({ active: false });
    totpSetupMock.mockResolvedValue({ setupId: 'sid', secret: 'ABCDEF', otpAuthUri: 'otpauth://x', qrImageBase64: 'qrb64' });
    setup('jwt');
    await screen.findByText(/nie jest skonfigurowany/i);
    fireEvent.click(screen.getByRole('button', { name: /Rozpocznij konfigurację/i }));
    await screen.findByText(/Secret:/i);
    expect(screen.getByText(/ABCDEF/)).toBeInTheDocument();
    expect(screen.getByText(/otpauth:\/\/x/)).toBeInTheDocument();
  });
  it('aktywuje TOTP przy poprawnym kodzie', async () => {
    totpStatusMock.mockResolvedValue({ active: false });
    totpSetupMock.mockResolvedValue({ setupId: 'sid', secret: 'ABC', otpAuthUri: 'uri', qrImageBase64: '' });
    totpEnableMock.mockResolvedValue({ message: 'TOTP włączony.' });
    setup('jwt');
    await screen.findByText(/nie jest skonfigurowany/i);
    fireEvent.click(screen.getByRole('button', { name: /Rozpocznij konfigurację/i }));
    await screen.findByText(/Secret:/i);
    fireEvent.change(screen.getByLabelText(/Kod z aplikacji/i), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: /Aktywuj/i }));
    await waitFor(() => expect(totpEnableMock).toHaveBeenCalledWith('jwt', 'sid', '123456'));
    expect(screen.getByText(/TOTP włączony/i)).toBeInTheDocument();
  });
  it('wyłącza TOTP gdy aktywny', async () => {
    totpStatusMock.mockResolvedValue({ active: true });
    totpDisableMock.mockResolvedValue({ message: 'OFF' });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    setup('jwt');
    await screen.findByText(/skonfigurowane/i);
    fireEvent.click(screen.getByRole('button', { name: /Wyłącz TOTP/i }));
    await waitFor(() => expect(totpDisableMock).toHaveBeenCalled());
    expect(screen.getByText(/OFF/i)).toBeInTheDocument();
  });
});
