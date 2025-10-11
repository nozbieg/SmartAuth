import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";

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
        setErr(null);
        setOkMsg(null);

        if (!accept) return setErr("Musisz zaakceptować regulamin.");
        if (password.length < 8) return setErr("Hasło musi mieć co najmniej 8 znaków.");
        if (password !== password2) return setErr("Hasła nie są identyczne.");

        setBusy(true);
        try {
            // Minimalny call – dostosuj do swojego backendu
            const res = await fetch("/api/auth/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email: email.trim(), password, displayName: displayName.trim() }),
            });

            if (!res.ok) {
                const t = await res.text();
                throw new Error(t || "Rejestracja nie powiodła się.");
            }

            // Opcja A: po rejestracji przenosimy do loginu z komunikatem
            setOkMsg("Konto utworzone. Możesz się zalogować.");
            setTimeout(() => nav("/login", { replace: true, state: { from } }), 800);

            // Opcja B (alternatywa): jeśli backend zwraca JWT, można od razu zalogować:
            // const data = await res.json();
            // if (data?.jwt) {
            //   localStorage.setItem("access_token", data.jwt);
            //   nav(from, { replace: true });
            // }
        } catch (e: any) {
            setErr(e?.message ?? "Rejestracja nie powiodła się.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
            <div className="w-full max-w-md bg-white rounded-2xl shadow p-6">
                <h1 className="text-2xl font-semibold mb-4">Rejestracja</h1>

                {err && <div className="mb-3 text-sm text-red-600">{err}</div>}
                {okMsg && <div className="mb-3 text-sm text-green-700">{okMsg}</div>}

                <form className="space-y-3" onSubmit={onSubmit}>
                    <input
                        className="w-full border rounded px-3 py-2"
                        placeholder="Adres email"
                        type="email"
                        autoComplete="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />

                    <input
                        className="w-full border rounded px-3 py-2"
                        placeholder="Imię i nazwisko / wyświetlana nazwa"
                        type="text"
                        value={displayName}
                        onChange={(e) => setDisplayName(e.target.value)}
                        required
                    />

                    <input
                        className="w-full border rounded px-3 py-2"
                        placeholder="Hasło (min. 8 znaków)"
                        type="password"
                        autoComplete="new-password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />

                    <input
                        className="w-full border rounded px-3 py-2"
                        placeholder="Powtórz hasło"
                        type="password"
                        autoComplete="new-password"
                        value={password2}
                        onChange={(e) => setPassword2(e.target.value)}
                        required
                    />

                    <label className="flex items-center gap-2 text-sm text-gray-700">
                        <input
                            type="checkbox"
                            checked={accept}
                            onChange={(e) => setAccept(e.target.checked)}
                        />
                        Akceptuję regulamin i politykę prywatności
                    </label>

                    <button
                        type="submit"
                        className="w-full rounded-xl py-2 shadow bg-black text-white disabled:opacity-60"
                        disabled={busy}
                    >
                        {busy ? "Rejestruję..." : "Utwórz konto"}
                    </button>
                </form>

                <div className="text-center text-sm mt-3">
                    Masz już konto?{" "}
                    <Link to="/login" className="text-blue-600 hover:underline">
                        Zaloguj się
                    </Link>
                </div>
            </div>
        </div>
    );
};

export default RegisterPage;
