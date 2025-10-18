import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import AuthLayout from "../../components/layout/AuthLayout";
import Button from "../../components/ui/Button";

const RegisterPage: React.FC = () => {
    const nav = useNavigate();
    const location = useLocation();
    const from = (location.state as any)?.from?.pathname || "/home";

    const [email, setEmail] = useState("");
    const [displayName, setDisplayName] = useState("");
    const [password, setPassword] = useState("");
    const [password2, setPassword2] = useState("");
    const [accept, setAccept] = useState(false);

    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState<string | null>(null);
    const [okMsg, setOkMsg] = useState<string | null>(null);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setErr(null); setOkMsg(null);

        if (!accept) return setErr("Musisz zaakceptować regulamin.");
        if (password.length < 8) return setErr("Hasło musi mieć co najmniej 8 znaków.");
        if (password !== password2) return setErr("Hasła nie są identyczne.");

        setBusy(true);
        try {
            const res = await fetch("/api/auth/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email: email.trim(), password, displayName: displayName.trim() }),
            });
            if (!res.ok) {
                const t = await res.text();
                throw new Error(t || "Rejestracja nie powiodła się.");
            }
            setOkMsg("Konto utworzone. Możesz się zalogować.");
            const delay = typeof import.meta !== 'undefined' && (import.meta as any).vitest ? 0 : 800;
            setTimeout(() => nav("/login", { replace: true, state: { from } }), delay);
        } catch (e: any) {
            setErr(e?.message ?? "Rejestracja nie powiodła się.");
        } finally { setBusy(false); }
    }

    return (
        <AuthLayout mediaTitle="Dołącz do SmartAuth" mediaDescription="Twórz konto aby korzystać z wieloskładnikowego, nowoczesnego uwierzytelniania.">
            <form onSubmit={onSubmit} className="form-stack" aria-describedby={err ? 'reg-error' : undefined}>
                <div className="auth-panel-header">
                    <h1>Rejestracja</h1>
                    <p className="subtitle">Wypełnij poniższe pola aby utworzyć konto.</p>
                </div>
                {err && <div id="reg-error" className="alert alert-danger" role="alert">{err}</div>}
                {okMsg && <div className="alert alert-success" role="status">{okMsg}</div>}

                <div className="form-grid">
                    <div className="form-control">
                        <label htmlFor="email">Email</label>
                        <input id="email" type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} required/>
                    </div>
                    <div className="form-control">
                        <label htmlFor="displayName">Imię i nazwisko / nazwa</label>
                        <input id="displayName" type="text" value={displayName} onChange={e => setDisplayName(e.target.value)} required/>
                    </div>
                    <div className="form-control">
                        <label htmlFor="password">Hasło (min. 8 znaków)</label>
                        <input id="password" type="password" autoComplete="new-password" value={password} onChange={e => setPassword(e.target.value)} required/>
                    </div>
                    <div className="form-control">
                        <label htmlFor="password2">Powtórz hasło</label>
                        <input id="password2" type="password" autoComplete="new-password" value={password2} onChange={e => setPassword2(e.target.value)} required/>
                    </div>
                </div>

                <label className="inline" style={{fontSize:'.75rem'}}>
                    <input type="checkbox" checked={accept} onChange={e => setAccept(e.target.checked)} />
                    <span>Akceptuję regulamin i politykę prywatności</span>
                </label>

                <div className="actions-col">
                    <Button type="submit" variant="primary" disabled={busy}>{busy ? 'Rejestruję...' : 'Utwórz konto'}</Button>
                    <div className="helper-text">Masz już konto? <Link to="/login" className="link-inline">Zaloguj się</Link></div>
                </div>
            </form>
        </AuthLayout>
    );
};

export default RegisterPage;
