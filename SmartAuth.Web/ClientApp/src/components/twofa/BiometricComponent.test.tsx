import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import BiometricComponent from './BiometricComponent';
import * as AuthService from '../../auth/AuthService';

vi.mock('../../auth/AuthService', () => ({
  getJwt: () => localStorage.getItem('access_token'),
  faceStatus: vi.fn(),
  faceEnroll: vi.fn(),
  faceDisable: vi.fn(),
  ApiError: class ApiError extends Error {}
}));

vi.mock('../ui/Card', () => ({ default: (p: any) => <div>{p.children}</div> }));
vi.mock('../ui/Button', () => ({ default: (p: any) => <button {...p}>{p.children}</button> }));

const faceStatusMock = AuthService.faceStatus as unknown as ReturnType<typeof vi.fn>;
const faceDisableMock = AuthService.faceDisable as unknown as ReturnType<typeof vi.fn>;

function renderWithToken(token: string | null = 'jwt') {
  if (token) localStorage.setItem('access_token', token); else localStorage.removeItem('access_token');
  return render(<BiometricComponent />);
}

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

describe('BiometricComponent', () => {
  it('zwraca null gdy brak jwt', () => {
    const { container } = renderWithToken(null);
    expect(container.firstChild).toBeNull();
  });

  it('pokazuje status i modal z prośbą o dostęp do kamery', async () => {
    faceStatusMock.mockResolvedValue({ enabled: false, activeCount: 0 });
    renderWithToken();
    await screen.findByText(/Brak zapisanej biometrii twarzy/i);
    fireEvent.click(screen.getByRole('button', { name: /Skonfiguruj biometrię/i }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('wyłącza aktywną biometrię', async () => {
    faceStatusMock
      .mockResolvedValueOnce({ enabled: true, activeCount: 1 })
      .mockResolvedValueOnce({ enabled: false, activeCount: 0 });
    faceDisableMock.mockResolvedValue({});
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderWithToken();
    await screen.findByText(/Biometria twarzy jest aktywna/i);
    fireEvent.click(screen.getByRole('button', { name: /Wyłącz biometrię/i }));
    await waitFor(() => expect(faceDisableMock).toHaveBeenCalled());
    await screen.findByText(/Biometria została wyłączona/i);
  });
});
