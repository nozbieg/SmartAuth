import React, {useEffect, useRef, useState} from "react";
import {useFeatureFlags} from "../../auth/FeatureFlagsContext";
import {loginWithPassword, saveJwt, verifyCode, verifyFace, verifyVoice,} from "../../auth/AuthService";
import {useNavigate, Link} from "react-router-dom";
import AuthLayout from "../../components/layout/AuthLayout";
import Button from "../../components/ui/Button";

// Kroki formularza
type Step = "credentials" | "twofa";
type TwoFAMethod = "code" | "face" | "voice";

const LoginPage: React.FC = () => {
    const nav = useNavigate();
    const {flags, loading} = useFeatureFlags();

    const [step, setStep] = useState<Step>("credentials");
    const [allowedMethods, setAllowed] = useState<TwoFAMethod[]>([]);
    const [selectedMethod, setSelected] = useState<TwoFAMethod | "">("");
    const [tempToken, setTempToken] = useState<string | null>(null);

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [code, setCode] = useState("");

    const [err, setErr] = useState<string | null>(null);
    const [info, setInfo] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    // Face upload
    const fileRef = useRef<HTMLInputElement | null>(null);

    // Voice recording
    const [recorder, setRecorder] = useState<MediaRecorder | null>(null);
    const chunksRef = useRef<BlobPart[]>([]);
    const streamRef = useRef<MediaStream | null>(null);
    const [voiceTimer, setVoiceTimer] = useState<number>(0);
    const timerRef = useRef<number | null>(null);

    useEffect(() => {
        return () => {
            recorder?.stop();
            streamRef.current?.getTracks().forEach((t) => t.stop());
            if (timerRef.current) window.clearInterval(timerRef.current);
        };
    }, [recorder]);

    // ------------------- Handlers -------------------
    async function onSubmitCredentials(e: React.FormEvent) {
        e.preventDefault();
        if (busy) return;
        setErr(null); setInfo(null);
        setBusy(true);
        try {
            const res = await loginWithPassword(email.trim(), password);
            if (res.requires2FA) {
                const methods = (res.methods ?? []).filter((m) => {
                    if (m === "code") return !!flags?.twofa_code;
                    if (m === "face") return !!flags?.twofa_face;
                    if (m === "voice") return !!flags?.twofa_voice;
                    return false;
                }) as TwoFAMethod[];
                if (methods.length === 0) {
                    setErr("Brak dostępnych metod 2FA. Skontaktuj się z administratorem.");
                    return;
                }
                setAllowed(methods);
                setSelected(methods[0]);
                setTempToken(res.tempToken ?? null);
                setStep("twofa");
                setInfo("Wprowadź drugi czynnik, aby zakończyć logowanie.");
            } else if (res.jwt) {
                saveJwt(res.jwt);
                nav("/home", {replace: true});
            } else {
                setErr("Nieprawidłowa odpowiedź logowania.");
            }
        } catch (e: any) {
            setErr(e?.message ?? "Błąd logowania");
        } finally {
            setBusy(false);
        }
    }

    async function onVerifyCode(e: React.FormEvent) {
        e.preventDefault();
        if (!tempToken || busy) return;
        setErr(null); setInfo(null);
        setBusy(true);
        try {
            const {jwt} = await verifyCode(tempToken, code.trim());
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) {
            setErr(e?.message ?? "Błąd weryfikacji kodu");
        } finally { setBusy(false); }
    }

    async function onVerifyFace(file: File) {
        if (!tempToken || busy) return;
        setErr(null); setInfo("Wysyłam i weryfikuję zdjęcie...");
        setBusy(true);
        try {
            const {jwt} = await verifyFace(tempToken, file);
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) { setErr(e?.message ?? "Błąd weryfikacji twarzy"); }
        finally {
            setBusy(false); setInfo(null); if (fileRef.current) fileRef.current.value = ""; }
    }

    async function onVerifyVoice(blob: Blob) {
        if (!tempToken) return;
        setErr(null); setInfo("Wysyłam nagranie...");
        setBusy(true);
        try {
            const {jwt} = await verifyVoice(tempToken, blob);
            saveJwt(jwt); nav("/home", {replace: true});
        } catch (e: any) { setErr(e?.message ?? "Błąd weryfikacji głosu"); }
        finally { setBusy(false); setInfo(null); }
    }

    async function startVoiceRecording() {
        try {
            setErr(null); setInfo("Nagrywanie rozpoczęte");
            const stream = await navigator.mediaDevices.getUserMedia({audio: true});
            streamRef.current = stream;
            const mime = MediaRecorder.isTypeSupported("audio/webm;codecs=opus") ? "audio/webm;codecs=opus" : "audio/webm";
            const rec = new MediaRecorder(stream, {mimeType: mime});
            chunksRef.current = [];
            rec.ondataavailable = (ev) => { if (ev.data && ev.data.size > 0) chunksRef.current.push(ev.data); };
            rec.onstop = async () => {
                if (timerRef.current) window.clearInterval(timerRef.current);
                const blob = new Blob(chunksRef.current, {type: mime});
                await onVerifyVoice(blob);
                stream.getTracks().forEach((t) => t.stop());
                streamRef.current = null;
                setVoiceTimer(0); setRecorder(null);
            };
            rec.start();
            setRecorder(rec);
            setVoiceTimer(0);
            timerRef.current = window.setInterval(() => {
                setVoiceTimer(prev => prev + 1);
            }, 1000);
        } catch {
            setErr("Brak dostępu do mikrofonu");
        }
    }

    function stopVoiceRecording() {
        recorder?.stop();
    }

    function resetToCredentials() {
        setStep("credentials");
        setTempToken(null);
        setAllowed([]);
        setSelected("");
        setCode("");
        setInfo(null); setErr(null);
    }

    // ------------------- UI fragments -------------------
    function renderCredentialsForm() {
        return (
            <form onSubmit={onSubmitCredentials} className="form-stack" aria-describedby={err ? "login-error" : undefined}>
                <div className="auth-panel-header">
                    <h1>Logowanie</h1>
                    <p className="subtitle">Wprowadź dane konta aby kontynuować.</p>
                </div>
                {err && <div id="login-error" className="alert alert-danger" role="alert">{err}</div>}
                <div className="form-grid">
                    <div className="form-control">
                        <label htmlFor="email">Email</label>
                        <input id="email" type="email" autoComplete="username" value={email} onChange={e => setEmail(e.target.value)} required/>
                    </div>
                    <div className="form-control">
                        <label htmlFor="password">Hasło</label>
                        <input id="password" type="password" autoComplete="current-password" value={password} onChange={e => setPassword(e.target.value)} required/>
                    </div>
                </div>
                <div className="actions-col">
                    <Button type="submit" variant="primary" disabled={busy}>{busy ? 'Logowanie...' : 'Zaloguj się'}</Button>
                    <div className="helper-text">Nie masz konta? <Link to="/register" className="link-inline">Zarejestruj się</Link></div>
                </div>
            </form>
        );
    }

    function renderMethodSelector() {
        return (
            <div className="twofa-methods" role="tablist" aria-label="Dostępne metody 2FA">
                {allowedMethods.map(m => (
                    <button
                        key={m}
                        type="button"
                        role="tab"
                        aria-selected={selectedMethod === m}
                        aria-pressed={selectedMethod === m}
                        className="btn method-btn"
                        onClick={() => setSelected(m)}
                        disabled={busy}
                    >
                        <small>Metoda</small>{m.toUpperCase()}
                    </button>
                ))}
            </div>
        );
    }

    function renderTwoFa() {
        return (
            <div className="form-stack">
                <div className="auth-panel-header">
                    <h1>Drugi krok</h1>
                    <p className="subtitle">Wybierz i potwierdź jedną z dostępnych metod.</p>
                </div>
                {err && <div className="alert alert-danger" role="alert">{err}</div>}
                {info && !err && <div className="alert alert-info" role="status">{info}</div>}
                {renderMethodSelector()}

                {selectedMethod === "code" && (
                    <form onSubmit={onVerifyCode} className="form-stack" aria-label="Weryfikacja kodu">
                        <div className="form-control">
                            <label htmlFor="twofa-code">Kod jednorazowy</label>
                            <input id="twofa-code" maxLength={6} inputMode="numeric" autoComplete="one-time-code" placeholder="123456" className="code-input" value={code} onChange={e => setCode(e.target.value)} required/>
                            <span className="helper-text">Sprawdź aplikację 2FA.</span>
                        </div>
                        <div className="split-actions">
                            <Button type="submit" variant="primary" disabled={busy}>{busy ? 'Weryfikuję...' : 'Potwierdź kod'}</Button>
                            <Button type="button" variant="outline" onClick={resetToCredentials} disabled={busy}>Wróć</Button>
                        </div>
                    </form>
                )}

                {selectedMethod === "face" && (
                    <div className="form-stack" aria-label="Weryfikacja twarzy">
                        <input ref={fileRef} type="file" accept="image/*" style={{display:'none'}} onChange={(e) => { const f = e.target.files?.[0]; if (f) onVerifyFace(f); }}/>
                        <div className="split-actions">
                            <Button type="button" variant="primary" onClick={() => fileRef.current?.click()} disabled={busy}>{busy ? 'Wysyłam...' : 'Prześlij zdjęcie twarzy'}</Button>
                            <Button type="button" variant="outline" onClick={resetToCredentials} disabled={busy}>Wróć</Button>
                        </div>
                    </div>
                )}

                {selectedMethod === "voice" && (
                    <div className="form-stack voice-rec" aria-label="Weryfikacja głosu">
                        <div className="voice-status">
                            <span className="dot-pulse" aria-hidden="true" style={{visibility: recorder ? 'visible' : 'hidden'}} />
                            {recorder ? <>
                                Nagrywanie... <span className="voice-timer">{String(Math.floor(voiceTimer/60)).padStart(2,'0')}:{String(voiceTimer%60).padStart(2,'0')}</span>
                            </> : 'Kliknij aby rozpocząć nagrywanie.'}
                        </div>
                        <div className="split-actions">
                            {!recorder ? (
                                <Button type="button" variant="primary" onClick={startVoiceRecording} disabled={busy}>Nagraj próbkę głosu</Button>
                            ) : (
                                <Button type="button" variant="danger" onClick={stopVoiceRecording} disabled={busy}>Zatrzymaj i wyślij</Button>
                            )}
                            <Button type="button" variant="outline" onClick={resetToCredentials} disabled={busy}>Wróć</Button>
                        </div>
                    </div>
                )}
            </div>
        );
    }

    // ------------------- Render root -------------------
    if (loading) {
        return (
            <AuthLayout>
                <div className="progress-inline" role="status"><span className="dot-pulse"/> Ładowanie...</div>
            </AuthLayout>
        );
    }

    if (!flags) {
        return (
            <AuthLayout>
                <div className="alert alert-danger">Błąd ładowania flag.</div>
            </AuthLayout>
        );
    }

    return (
        <AuthLayout mediaTitle="Bezpieczne logowanie" mediaDescription="Uwierzytelniaj się przy pomocy hasła oraz metod 2FA: kodów TOTP, biometrii twarzy lub głosu.">
            {step === "credentials" && renderCredentialsForm()}
            {step === "twofa" && renderTwoFa()}
        </AuthLayout>
    );
};

export default LoginPage;
