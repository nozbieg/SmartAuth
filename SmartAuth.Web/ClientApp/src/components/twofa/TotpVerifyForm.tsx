import React, { useState, useEffect, useRef } from 'react';
import Button from '../../components/ui/Button';
import { ApiError, verifyCode } from '../../auth/AuthService';

export interface TotpVerifyFormProps {
  tempToken: string;
  busy?: boolean;
  onSuccess: (jwt: string) => void;
  onCancel: () => void;
  onBusyChange?: (b: boolean) => void;
  disabled?: boolean;
}

const TotpVerifyForm: React.FC<TotpVerifyFormProps> = ({ tempToken, busy: busyProp, onSuccess, onCancel, onBusyChange, disabled }) => {
  const [code, setCode] = useState('');
  const [err, setErr] = useState<string | null>(null);
  const [busyLocal, setBusyLocal] = useState(false);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const busy = busyProp || busyLocal;

  useEffect(() => { inputRef.current?.focus(); }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!tempToken || busy || code.length !== 6) return;
    setErr(null);
    setBusyLocal(true); onBusyChange?.(true);
    try {
      const { jwt } = await verifyCode(tempToken, code.trim());
      onSuccess(jwt);
    } catch (e:any) {
      if (e instanceof ApiError) setErr(e.message); else setErr(e?.message || 'Błąd weryfikacji TOTP');
    } finally { setBusyLocal(false); onBusyChange?.(false); }
  }

  function onChange(val: string) {
    const digits = val.replace(/[^0-9]/g,'').slice(0,6);
    setCode(digits);
  }

  return (
    <form onSubmit={handleSubmit} className="totp-form" aria-label="Weryfikacja TOTP">
      <div className="form-control">
        <label htmlFor="totp-code">Kod TOTP</label>
        <input
          ref={inputRef}
          id="totp-code"
          className="code-input"
          inputMode="numeric"
          autoComplete="one-time-code"
          placeholder="123456"
          maxLength={6}
          value={code}
          onChange={e => onChange(e.target.value)}
          disabled={disabled || busy}
          required
        />
        <span className="helper-text">Otwórz Microsoft Authenticator i przepisz bieżący 6‑cyfrowy kod.</span>
        {err && <span className="field-error" role="alert">{err}</span>}
      </div>
      <div className="form-actions">
        <Button type="submit" variant="primary" disabled={busy || code.length !== 6 || disabled}>{busy ? 'Weryfikuję...' : 'Potwierdź TOTP'}</Button>
        <Button type="button" variant="outline" onClick={onCancel} disabled={busy}>Wróć</Button>
      </div>
    </form>
  );
};

export default TotpVerifyForm;
