import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { render, screen } from '@testing-library/react';
import { RouteGuard } from './RouteGuard';

const getJwtMock = vi.fn();
const isJwtValidMock = vi.fn();

vi.mock('./AuthService', () => ({ getJwt: () => getJwtMock(), isJwtValid: (...a: any[]) => isJwtValidMock(...a) }));

function Protected() { return <div data-testid="protected">OK</div>; }
function Login() { return <div data-testid="login">LOGIN</div>; }

describe('RouteGuard', () => {
  it('przepuszcza przy ważnym tokenie', () => {
    getJwtMock.mockReturnValue('jwt');
    isJwtValidMock.mockReturnValue(true);
    render(<MemoryRouter initialEntries={['/home']}><Routes><Route element={<RouteGuard />}> <Route path="/home" element={<Protected/>} /> </Route><Route path="/login" element={<Login/>} /></Routes></MemoryRouter>);
    expect(screen.getByTestId('protected')).toBeInTheDocument();
    expect(screen.queryByTestId('login')).toBeNull();
  });
  it('przekierowuje na /login przy nieważnym tokenie', () => {
    getJwtMock.mockReturnValue(null);
    isJwtValidMock.mockReturnValue(false);
    render(<MemoryRouter initialEntries={['/home']}><Routes><Route element={<RouteGuard />}> <Route path="/home" element={<Protected/>} /> </Route><Route path="/login" element={<Login/>} /></Routes></MemoryRouter>);
    expect(screen.getByTestId('login')).toBeInTheDocument();
    expect(screen.queryByTestId('protected')).toBeNull();
  });
});

