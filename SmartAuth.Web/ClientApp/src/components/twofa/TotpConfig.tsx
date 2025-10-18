import React, { useEffect, useState } from 'react';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import { getJwt, totpStatus, totpSetup, totpEnable, totpDisable, ApiError } from '../../auth/AuthService';

const TotpConfig: React.FC = () => {
  const jwt = getJwt();
  const [loading, setLoading] = useState(true);
  const [active, setActive] = useState(false);
  const [setupId, setSetupId] = useState<string | null>(null);
  const [secret, setSecret] = useState<string | null>(null);
  const [otpAuthUri, setOtpAuthUri] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [qrBase64, setQrBase64] = useState<string | null>(null);

  useEffect(() => {
    if (!jwt) { setLoading(false); return; }
    (async () => {
      try {
        const { active } = await totpStatus(jwt);
        setActive(active);
      } catch (e:any) {
      } finally { setLoading(false); }
    })();
  }, [jwt]);

  async function startSetup() {
    if (!jwt || busy) return;
    setBusy(true); setErr(null); setInfo(null);
    try {
      const res = await totpSetup(jwt, false);
      setSetupId(res.setupId); setSecret(res.secret); setOtpAuthUri(res.otpAuthUri); setQrBase64(res.qrImageBase64);
      setInfo('Zeskanuj kod w aplikacji uwierzytelniającej, następnie wpisz wygenerowany 6-cyfrowy kod aby aktywować.');
    } catch (e:any) {
      setErr(e?.message || 'Błąd inicjacji TOTP');
    } finally { setBusy(false); }
  }

  async function confirmEnable(e: React.FormEvent) {
    e.preventDefault();
    if (!jwt || !setupId || busy) return;
    setBusy(true); setErr(null); setInfo(null);
    try {
      const res = await totpEnable(jwt, setupId, code.trim());
      setActive(true); setSetupId(null); setSecret(null); setOtpAuthUri(null); setCode('');
      setInfo(res.message || 'TOTP włączony.');
    } catch (e:any) {
      if (e instanceof ApiError) setErr(e.message); else setErr('Nie udało się aktywować TOTP');
    } finally { setBusy(false); }
  }

  async function disableTotp() {
    if (!jwt || busy) return;
    if (!window.confirm('Wyłączyć TOTP?')) return;
    setBusy(true); setErr(null); setInfo(null);
    try {
      const res = await totpDisable(jwt);
      setActive(false); setInfo(res.message || 'TOTP wyłączony.');
    } catch (e:any) {
      setErr(e?.message || 'Błąd wyłączania');
    } finally { setBusy(false); }
  }

  async function restartSetup() {
    if (!jwt || busy) return;
    setBusy(true); setErr(null); setInfo(null);
    try {
      const res = await totpSetup(jwt, true);
      setSetupId(res.setupId); setSecret(res.secret); setOtpAuthUri(res.otpAuthUri); setQrBase64(res.qrImageBase64); setCode('');
      setInfo('Konfiguracja zrestartowana. Zeskanuj nowy kod QR.');
    } catch (e:any) { setErr(e?.message || 'Błąd restartu'); } finally { setBusy(false); }
  }

  function cancelSetup() {
    setSetupId(null); setSecret(null); setOtpAuthUri(null); setCode(''); setErr(null); setInfo(null);
  }

  if (!jwt) return null;
  if (loading) return <Card title="2FA TOTP" headingLevel={2}><div className="progress-inline"><span className="dot-pulse"/> Ładowanie...</div></Card>;

  return (
    <Card title="2FA TOTP" headingLevel={2}>
      {err && <div className="alert alert-danger" role="alert">{err}</div>}
      {info && !err && <div className="alert alert-info" role="status">{info}</div>}
      {active && !setupId && (
        <div className="card-section">
          <p><strong>TOTP jest skonfigurowane.</strong> Możesz wyłączyć tę metodę uwierzytelniania, jeśli nie chcesz już z niej korzystać.</p>
          <div className="form-actions">
            <Button variant="outline" disabled={busy} onClick={disableTotp}>{busy ? 'Przetwarzanie...' : 'Wyłącz TOTP'}</Button>
          </div>
        </div>
      )}
      {!active && !setupId && (
        <div className="card-section">
          <p>TOTP nie jest skonfigurowany.</p>
          <div className="form-actions">
            <Button variant="primary" disabled={busy} onClick={startSetup}>{busy ? 'Przygotowywanie...' : 'Rozpocznij konfigurację'}</Button>
          </div>
        </div>
      )}
      {!active && setupId && (
        <form onSubmit={confirmEnable} className="card-section" aria-label="Aktywacja TOTP">
          <p><strong>Secret:</strong> <code className="code-secret">{secret}</code></p>
          {qrBase64 && <div className="qr-wrapper" aria-label="Kod QR TOTP">
            <img alt="QR kod TOTP" src={`data:image/png;base64,${qrBase64}`} />
          </div>}
          <p><strong>URI:</strong> <code className="code-uri">{otpAuthUri}</code></p>
          <div className="form-control">
            <label htmlFor="totp-code">Kod z aplikacji</label>
            <input id="totp-code" className="code-input" maxLength={6} inputMode="numeric" autoComplete="one-time-code" value={code}
                   onChange={e => setCode(e.target.value)} placeholder="123456" required />
          </div>
          <div className="form-actions">
            <Button type="submit" variant="primary" disabled={busy}>{busy ? 'Włączam...' : 'Aktywuj'}</Button>
            <Button type="button" variant="outline" disabled={busy} onClick={cancelSetup}>Anuluj</Button>
            <Button type="button" variant="danger" disabled={busy} onClick={restartSetup}>Restartuj</Button>
          </div>
        </form>
      )}
    </Card>
  );
};

export default TotpConfig;
