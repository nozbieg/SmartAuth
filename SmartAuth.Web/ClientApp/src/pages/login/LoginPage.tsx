import React, {useState} from "react";
import {useFeatureFlags} from "../../auth/FeatureFlagsContext";
import {loginWithPassword, saveJwt, verifyCode, ApiError} from "../../auth/AuthService";
import {useNavigate, Link} from "react-router-dom";
import AuthLayout from "../../components/layout/AuthLayout";
import Button from "../../components/ui/Button";

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
    const [fieldErrors, setFieldErrors] = useState<Record<string,string>>({});

    async function onSubmitCredentials(e: React.FormEvent) {
        e.preventDefault();
        if (busy) return;
        setErr(null);
        setInfo(null);
        setFieldErrors({});
        setBusy(true);
        try {
            const res = await loginWithPassword(email.trim(), password); // LoginResponse
            if (res.requires2Fa) {
                const methods = (res.methods ?? []).filter(m => m === "code" && !!flags?.twofa_code) as TwoFAMethod[];
                if (methods.length === 0) {
                    setErr("Brak dostępnych metod 2FA.");
                    return;
                }
                if (!res.token) {
                    setErr("Brak tymczasowego tokena 2FA.");
                    return;
                }
                setAllowed(methods);
                setSelected(methods[0]);
                setTempToken(res.token); // tymczasowy token
                setStep("twofa");
                setInfo("Wprowadź drugi czynnik, aby zakończyć logowanie.");
            } else {
                if (!res.token) {
                    setErr("Brak tokena JWT w odpowiedzi.");
                    return;
                }
                saveJwt(res.token); // końcowy JWT
                nav("/home", {replace: true});
            }
        } catch (e: any) {
            if (e instanceof ApiError) {
                setErr(e.message);
                if (e.metadata) setFieldErrors(e.metadata);
            } else {
                setErr(e?.message ?? "Błąd logowania");
            }
        } finally {
            setBusy(false);
        }
    }

    async function onVerifyCode(e: React.FormEvent) {
        e.preventDefault();
        if (!tempToken || busy) return;
        setErr(null);
        setInfo(null);
        setFieldErrors({});
        setBusy(true);
        try {
            const {jwt} = await verifyCode(tempToken, code.trim());
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) {
            if (e instanceof ApiError) {
                setErr(e.message);
                if (e.metadata) setFieldErrors(e.metadata);
            } else {
                setErr(e?.message ?? "Błąd weryfikacji kodu");
            }
        } finally {
            setBusy(false);
        }
    }

    function resetToCredentials() {
        setStep("credentials");
        setTempToken(null);
        setAllowed([]);
        setSelected("");
        setCode("");
        setInfo(null);
        setErr(null);
        setFieldErrors({});
    }

    // ------------------- UI fragments -------------------
    function renderCredentialsForm() {
        return (
            <form onSubmit={onSubmitCredentials} className="form-stack"
                  aria-describedby={err ? "login-error" : undefined}>
                <div className="auth-panel-header">
                    <h1>Logowanie</h1>
                    <p className="subtitle">Wprowadź dane konta aby kontynuować.</p>
                </div>
                {err && <div id="login-error" className="alert alert-danger" role="alert">{err}</div>}
                <div className="form-grid">
                    <div className="form-control">
                        <label htmlFor="email">Email</label>
                        <input id="email" type="email" autoComplete="username" value={email}
                               aria-invalid={!!fieldErrors.Email || !!fieldErrors.email}
                               onChange={e => setEmail(e.target.value)} required/>
                        { (fieldErrors.Email || fieldErrors.email) && <small className="field-error">{fieldErrors.Email || fieldErrors.email}</small> }
                    </div>
                    <div className="form-control">
                        <label htmlFor="password">Hasło</label>
                        <input id="password" type="password" autoComplete="current-password" value={password}
                               aria-invalid={!!fieldErrors.Password || !!fieldErrors.password}
                               onChange={e => setPassword(e.target.value)} required/>
                        { (fieldErrors.Password || fieldErrors.password) && <small className="field-error">{fieldErrors.Password || fieldErrors.password}</small> }
                    </div>
                </div>
                <div className="actions-col">
                    <Button type="submit" variant="primary"
                            disabled={busy}>{busy ? 'Logowanie...' : 'Zaloguj się'}</Button>
                    <div className="helper-text">Nie masz konta? <Link to="/register" className="link-inline">Zarejestruj
                        się</Link></div>
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
                            <input id="twofa-code" maxLength={6} inputMode="numeric" autoComplete="one-time-code"
                                   placeholder="123456" className="code-input" value={code}
                                   aria-invalid={!!fieldErrors.Code || !!fieldErrors.code}
                                   onChange={e => setCode(e.target.value)} required/>
                            { (fieldErrors.Code || fieldErrors.code) && <small className="field-error">{fieldErrors.Code || fieldErrors.code}</small> }
                            <span className="helper-text">Sprawdź aplikację 2FA.</span>
                        </div>
                        <div className="split-actions">
                            <Button type="submit" variant="primary"
                                    disabled={busy}>{busy ? 'Weryfikuję...' : 'Potwierdź kod'}</Button>
                            <Button type="button" variant="outline" onClick={resetToCredentials}
                                    disabled={busy}>Wróć</Button>
                        </div>
                    </form>
                )}
            </div>
        );
    }

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
        <AuthLayout mediaTitle="Bezpieczne logowanie"
                    mediaDescription="Uwierzytelniaj się przy pomocy hasła oraz metod 2FA: kodów TOTP, biometrii twarzy lub głosu.">
            {step === "credentials" && renderCredentialsForm()}
            {step === "twofa" && renderTwoFa()}
        </AuthLayout>
    );
};

export default LoginPage;
