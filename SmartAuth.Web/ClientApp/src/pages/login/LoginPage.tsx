import React, {useEffect, useRef, useState} from "react";
import {useFeatureFlags} from "../../auth/FeatureFlagsContext";
import {
    loginWithPassword,
    saveJwt,
    verifyCode,
    verifyFace,
    verifyVoice,
} from "../../auth/AuthService";
import {useNavigate, Link} from "react-router-dom";

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
    const [busy, setBusy] = useState(false);

    // face upload
    const fileRef = useRef<HTMLInputElement | null>(null);

    // voice recording
    const [recorder, setRecorder] = useState<MediaRecorder | null>(null);
    const chunksRef = useRef<BlobPart[]>([]);
    const streamRef = useRef<MediaStream | null>(null);

    useEffect(() => {
        return () => {
            // cleanup on unmount
            recorder?.stop();
            streamRef.current?.getTracks().forEach((t) => t.stop());
        };
    }, [recorder]);

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center p-6">
                Ładowanie…
            </div>
        );
    }

    if (!flags) {
        return (
            <div className="min-h-screen flex items-center justify-center p-6 text-red-600">
                Błąd ładowania flag.
            </div>
        );
    }

    async function onSubmitCredentials(e: React.FormEvent) {
        e.preventDefault();
        if (busy) return;
        setErr(null);
        setBusy(true);
        try {
            const res = await loginWithPassword(email.trim(), password);
            console.log(res);
            if (res.requires2FA) {
                // Filtrujemy metodami dostępnymi po stronie backendu i dozwolonymi przez feature flags.
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
        setErr(null);
        setBusy(true);
        try {
            const {jwt} = await verifyCode(tempToken, code.trim());
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) {
            setErr(e?.message ?? "Błąd weryfikacji kodu");
        } finally {
            setBusy(false);
        }
    }

    async function onVerifyFace(file: File) {
        if (!tempToken || busy) return;
        setErr(null);
        setBusy(true);
        try {
            const {jwt} = await verifyFace(tempToken, file);
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) {
            setErr(e?.message ?? "Błąd weryfikacji twarzy");
        } finally {
            setBusy(false);
            if (fileRef.current) fileRef.current.value = "";
        }
    }

    async function onVerifyVoice(blob: Blob) {
        if (!tempToken || busy) return;
        setErr(null);
        setBusy(true);
        try {
            const {jwt} = await verifyVoice(tempToken, blob);
            saveJwt(jwt);
            nav("/home", {replace: true});
        } catch (e: any) {
            setErr(e?.message ?? "Błąd weryfikacji głosu");
        } finally {
            setBusy(false);
        }
    }

    async function startVoiceRecording() {
        try {
            setErr(null);
            // uzyskaj strumień audio
            const stream = await navigator.mediaDevices.getUserMedia({audio: true});
            streamRef.current = stream;

            // typ nagrania (szerokie wsparcie)
            const mime = MediaRecorder.isTypeSupported("audio/webm;codecs=opus")
                ? "audio/webm;codecs=opus"
                : "audio/webm";

            const rec = new MediaRecorder(stream, {mimeType: mime});
            chunksRef.current = [];
            rec.ondataavailable = (ev) => {
                if (ev.data && ev.data.size > 0) chunksRef.current.push(ev.data);
            };
            rec.onstop = async () => {
                const blob = new Blob(chunksRef.current, {type: mime});
                await onVerifyVoice(blob);
                // posprzątaj stream
                stream.getTracks().forEach((t) => t.stop());
                streamRef.current = null;
            };
            rec.start();
            setRecorder(rec);
        } catch {
            setErr("Brak dostępu do mikrofonu");
        }
    }

    function stopVoiceRecording() {
        // stop() wywoła onstop → wyśle blob i posprząta stream
        recorder?.stop();
        setRecorder(null);
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
            <div className="w-full max-w-md bg-white rounded-2xl shadow p-6">
                <h1 className="text-2xl font-semibold mb-4">Logowanie</h1>

                {err && <div className="mb-3 text-sm text-red-600">{err}</div>}

                {step === "credentials" && (
                    <form onSubmit={onSubmitCredentials} className="space-y-3">
                        <input
                            className="w-full border rounded px-3 py-2"
                            placeholder="Email"
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            autoComplete="username"
                            required
                        />
                        <input
                            className="w-full border rounded px-3 py-2"
                            placeholder="Hasło"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            autoComplete="current-password"
                            required
                        />

                        <button
                            type="submit"
                            className="w-full rounded-xl py-2 shadow bg-black text-white disabled:opacity-60"
                            disabled={busy}
                        >
                            {busy ? "Logowanie..." : "Zaloguj"}
                        </button>

                        {/* Register button */}
                        <div className="text-center text-sm mt-2">
                            Nie masz konta?{" "}
                            <Link
                                to="/register"
                                className="text-blue-600 hover:underline"
                            >
                                Zarejestruj się
                            </Link>
                        </div>
                    </form>
                )}

                {step === "twofa" && (
                    <div className="space-y-4">
                        <div>
                            <label className="text-sm text-gray-600">Metoda 2FA</label>
                            <select
                                className="w-full border rounded px-3 py-2 mt-1"
                                value={selectedMethod}
                                onChange={(e) => setSelected(e.target.value as TwoFAMethod)}
                            >
                                {allowedMethods.map((m) => (
                                    <option key={m} value={m}>
                                        {m.toUpperCase()}
                                    </option>
                                ))}
                            </select>
                        </div>

                        {selectedMethod === "code" && (
                            <form onSubmit={onVerifyCode} className="space-y-3">
                                <input
                                    className="w-full border rounded px-3 py-2 tracking-widest"
                                    maxLength={6}
                                    placeholder="Kod 2FA"
                                    value={code}
                                    onChange={(e) => setCode(e.target.value)}
                                    inputMode="numeric"
                                    autoComplete="one-time-code"
                                    required
                                />
                                <button
                                    className="w-full rounded-xl py-2 shadow bg-black text-white disabled:opacity-60"
                                    type="submit"
                                    disabled={busy}
                                >
                                    {busy ? "Weryfikuję..." : "Potwierdź kod"}
                                </button>
                            </form>
                        )}

                        {selectedMethod === "face" && (
                            <div className="space-y-3">
                                <input
                                    ref={fileRef}
                                    type="file"
                                    accept="image/*"
                                    className="hidden"
                                    onChange={(e) => {
                                        const f = e.target.files?.[0];
                                        if (f) onVerifyFace(f);
                                    }}
                                />
                                <button
                                    className="w-full rounded-xl py-2 shadow bg-black text-white disabled:opacity-60"
                                    onClick={() => fileRef.current?.click()}
                                    disabled={busy}
                                >
                                    Prześlij zdjęcie twarzy
                                </button>
                            </div>
                        )}

                        {selectedMethod === "voice" && (
                            <div className="space-y-3">
                                {!recorder ? (
                                    <button
                                        className="w-full rounded-xl py-2 shadow bg-black text-white disabled:opacity-60"
                                        onClick={startVoiceRecording}
                                        disabled={busy}
                                    >
                                        Nagraj próbkę głosu
                                    </button>
                                ) : (
                                    <button
                                        className="w-full rounded-xl py-2 shadow bg-red-600 text-white disabled:opacity-60"
                                        onClick={stopVoiceRecording}
                                        disabled={busy}
                                    >
                                        Zatrzymaj i wyślij
                                    </button>
                                )}
                            </div>
                        )}

                        <div className="pt-2">
                            <button
                                className="w-full rounded-xl py-2 border border-gray-300"
                                onClick={() => {
                                    // pozwól wrócić, np. jeśli user zmienił zdanie
                                    setStep("credentials");
                                    setTempToken(null);
                                    setAllowed([]);
                                    setSelected("");
                                }}
                            >
                                Wróć
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default LoginPage;
