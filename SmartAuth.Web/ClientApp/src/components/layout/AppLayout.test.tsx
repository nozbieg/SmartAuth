import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import AppLayout from './AppLayout';

const mockNavigate = vi.fn();
const mockLogout = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual: any = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});
vi.mock('../../auth/AuthService', () => ({ logout: () => mockLogout() }));
vi.mock('../ui/Button', () => ({ default: (p: any) => <button {...p} /> }));

beforeEach(() => { mockNavigate.mockReset(); mockLogout.mockReset(); });

describe('AppLayout', () => {
  it('renderuje tytuł i dzieci', () => {
    render(<AppLayout title="Panel"><div data-testid="ct">Treść</div></AppLayout>);
    expect(screen.getByText('Panel')).toBeInTheDocument();
    expect(screen.getByTestId('ct')).toBeInTheDocument();
  });
  it('nawiguje po kliknięciu w brand', () => {
    render(<AppLayout title="Panel">X</AppLayout>);
    fireEvent.click(screen.getByText('Panel'));
    expect(mockNavigate).toHaveBeenCalledWith('/home');
  });
  it('wylogowuje i przekierowuje', () => {
    render(<AppLayout>Y</AppLayout>);
    fireEvent.click(screen.getByRole('button', { name: /Wyloguj/i }));
    expect(mockLogout).toHaveBeenCalled();
    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true });
  });
});

