import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LandingPage from './LandingPage';

const logoutMock = vi.fn();
const navigateMock = vi.fn();

type Claims = { sub: string; exp: number; name?: string; email?: string; role?: any };

vi.mock('../../auth/AuthService', () => ({ getJwt: () => localStorage.getItem('access_token'), logout: () => logoutMock() }));
vi.mock('react-router-dom', async () => {
  const actual: any = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => navigateMock };
});

const jwtDecodeMock = vi.fn();
vi.mock('jwt-decode', () => ({ jwtDecode: (t: string) => jwtDecodeMock(t) }));
vi.mock('../../components/layout/AppLayout', () => ({ default: ({ children, title }: any) => <div><h1>{title}</h1>{children}</div> }));
vi.mock('../../components/ui/Card', () => ({ default: (p: any) => <div>{p.children}</div> }));
vi.mock('../../components/twofa/TotpConfig', () => ({ default: () => <div data-testid="totp" /> }));
vi.mock('../../components/twofa/BiometricComponent', () => ({ default: () => <div data-testid="biometric" /> }));

function makeToken(payload: Claims) {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.`;
}

beforeEach(() => { localStorage.clear(); vi.clearAllMocks(); });

describe('LandingPage', () => {
  it('renderuje dane użytkownika z claimami', () => {
    const token = makeToken({ sub: '123', exp: Math.floor(Date.now()/1000)+60, name: 'Jan', email: 'jan@example.com', role: ['admin','user'] });
    localStorage.setItem('access_token', token);
    jwtDecodeMock.mockImplementation(() => ({ sub: '123', exp: Math.floor(Date.now()/1000)+60, name: 'Jan', email: 'jan@example.com', role: ['admin','user'] }));
    render(<MemoryRouter><LandingPage /></MemoryRouter>);
    expect(screen.getByRole('heading', { name: /Witaj, Jan/i })).toBeInTheDocument();
    expect(screen.getByText('jan@example.com')).toBeInTheDocument();
    expect(screen.getByText(/admin, user/)).toBeInTheDocument();
    expect(screen.getByTestId('totp')).toBeInTheDocument();
    expect(screen.getByTestId('biometric')).toBeInTheDocument();
  });
  it('wylogowuje przy uszkodzonym tokenie', () => {
    localStorage.setItem('access_token', 'bad');
    jwtDecodeMock.mockImplementation(() => { throw new Error('bad'); });
    render(<MemoryRouter><LandingPage /></MemoryRouter>);
    expect(logoutMock).toHaveBeenCalled();
    expect(navigateMock).toHaveBeenCalledWith('/login', { replace: true });
  });
  it('obsługuje pojedynczą rolę jako string', () => {
    const token = makeToken({ sub: '1', exp: Math.floor(Date.now()/1000)+60, role: 'manager' });
    localStorage.setItem('access_token', token);
    jwtDecodeMock.mockImplementation(() => ({ sub: '1', exp: Math.floor(Date.now()/1000)+60, role: 'manager' }));
    render(<MemoryRouter><LandingPage /></MemoryRouter>);
    expect(screen.getByText(/manager/)).toBeInTheDocument();
  });
});
