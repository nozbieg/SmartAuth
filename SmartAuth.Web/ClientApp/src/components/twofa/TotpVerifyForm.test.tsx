import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import TotpVerifyForm from './TotpVerifyForm';
import * as AuthService from '../../auth/AuthService';

vi.mock('../../auth/AuthService', () => {
  return {
    verifyCode: vi.fn(),
    ApiError: class ApiError extends Error {}
  };
});

vi.mock('../ui/Button', () => ({ default: (p: any) => <button {...p} /> }));

beforeEach(() => {
  (AuthService.verifyCode as any).mockReset();
});

describe('TotpVerifyForm', () => {
  it('auto-focusuje input', () => {
    render(<TotpVerifyForm tempToken="tt" onSuccess={()=>{}} onCancel={()=>{}} />);
    const input = screen.getByLabelText(/Kod TOTP/i) as HTMLInputElement;
    expect(document.activeElement).toBe(input);
  });
  it('sanityzuje wpis do cyfr i limituje do 6', () => {
    render(<TotpVerifyForm tempToken="tt" onSuccess={()=>{}} onCancel={()=>{}} />);
    const input = screen.getByLabelText(/Kod TOTP/i) as HTMLInputElement;
    fireEvent.change(input, { target: { value: '12a34b789' } });
    expect(input.value).toBe('123478');
  });
  it('nie submituje gdy kod <6', () => {
    render(<TotpVerifyForm tempToken="tt" onSuccess={()=>{}} onCancel={()=>{}} />);
    fireEvent.change(screen.getByLabelText(/Kod TOTP/i), { target: { value: '12345' } });
    fireEvent.click(screen.getByRole('button', { name: /Potwierdź TOTP/i }));
    expect(AuthService.verifyCode).not.toHaveBeenCalled();
  });
  it('wysyła verifyCode przy pełnym kodzie', async () => {
    (AuthService.verifyCode as any).mockResolvedValue({ jwt: 'final' });
    const onSuccess = vi.fn();
    render(<TotpVerifyForm tempToken="temp" onSuccess={onSuccess} onCancel={()=>{}} />);
    fireEvent.change(screen.getByLabelText(/Kod TOTP/i), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: /Potwierdź TOTP/i }));
    await waitFor(() => expect(AuthService.verifyCode).toHaveBeenCalledWith('temp', '123456'));
    expect(onSuccess).toHaveBeenCalledWith('final');
  });
  it('pokazuje błąd ApiError', async () => {
    (AuthService.verifyCode as any).mockRejectedValue(new (AuthService.ApiError as any)('Źly kod'));
    render(<TotpVerifyForm tempToken="temp" onSuccess={()=>{}} onCancel={()=>{}} />);
    fireEvent.change(screen.getByLabelText(/Kod TOTP/i), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: /Potwierdź TOTP/i }));
    expect(await screen.findByText(/Źly kod/i)).toBeInTheDocument();
  });
});
