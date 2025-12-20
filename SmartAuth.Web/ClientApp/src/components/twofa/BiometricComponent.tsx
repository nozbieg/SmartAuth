import React, { useEffect, useRef, useState } from "react";
import Card from "../ui/Card";
import Button from "../ui/Button";
import { ApiError, faceDisable, faceEnroll, faceStatus, getJwt, type FaceEnrollResponse, type FaceStatusResponse } from "../../auth/AuthService";
import { encodeRgbPayload, ensureCanvas } from "../../commons/facePayload";

const BiometricComponent: React.FC = () => {
  const jwt = getJwt();
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState<FaceStatusResponse | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [showPermission, setShowPermission] = useState(false);
  const [analysis, setAnalysis] = useState<FaceEnrollResponse | null>(null);

  useEffect(() => {
    if (!jwt) return;
    let active = true;
    (async () => {
      try {
        const current = await faceStatus(jwt);
        if (active) setStatus(current);
      } catch (e: any) {
        if (active) setErr(e?.message || "Błąd pobierania statusu biometrii");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [jwt]);

  useEffect(() => {
    const video = videoRef.current;
    if (video && stream) {
      video.srcObject = stream;
      video.play().catch(() => undefined);
    }
    return () => {
      if (video) video.srcObject = null;
    };
  }, [stream]);

  useEffect(() => () => stopStream(), []);

  if (!jwt) return null;

  async function refreshStatus() {
    try {
      const current = await faceStatus(jwt);
      setStatus(current);
    } catch (e: any) {
      setErr(e?.message || "Błąd odświeżania statusu");
    }
  }

  async function startCamera() {
    if (!navigator.mediaDevices?.getUserMedia) {
      setErr("Twoja przeglądarka nie udostępnia API kamery.");
      setShowPermission(false);
      return;
    }
    setBusy(true);
    setErr(null);
    setInfo(null);
    setAnalysis(null);
    try {
      const media = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "user", width: { ideal: 640 }, height: { ideal: 480 } }
      });
      setStream(media);
    } catch (e: any) {
      setErr(e?.message || "Nie udało się uzyskać dostępu do kamery.");
    } finally {
      setShowPermission(false);
      setBusy(false);
    }
  }

  function stopStream() {
    stream?.getTracks().forEach(t => t.stop());
    setStream(null);
  }

  async function captureAndEnroll() {
    if (!jwt || !videoRef.current) return;
    setBusy(true);
    setErr(null);
    setInfo(null);
    try {
      const video = videoRef.current;
      const canvas = ensureCanvas(canvasRef);
      const width = video.videoWidth || 640;
      const height = video.videoHeight || 480;
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext("2d", { willReadFrequently: true });
      if (!ctx) throw new Error("Brak dostępu do kontekstu rysowania");
      ctx.drawImage(video, 0, 0, width, height);
      const payload = encodeRgbPayload(ctx.getImageData(0, 0, width, height));
      const res = await faceEnroll(jwt, payload);
      setAnalysis(res);
      setInfo("Nowa próbka biometryczna została zapisana.");
      await refreshStatus();
    } catch (e: any) {
      if (e instanceof ApiError) setErr(e.message); else setErr(e?.message || "Nie udało się zapisać biometrii.");
    } finally {
      setBusy(false);
      stopStream();
    }
  }

  async function disableBiometrics() {
    if (!jwt || busy) return;
    if (!window.confirm("Wyłączyć biometrię twarzy?")) return;
    setBusy(true);
    setErr(null);
    setInfo(null);
    try {
      await faceDisable(jwt);
      await refreshStatus();
      setAnalysis(null);
      setInfo("Biometria została wyłączona.");
    } catch (e: any) {
      setErr(e?.message || "Nie udało się wyłączyć biometrii");
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return <Card title="Biometria twarzy" headingLevel={2}><div className="progress-inline"><span className="dot-pulse"/> Ładowanie...</div></Card>;
  }

  return (
    <Card title="Biometria twarzy" headingLevel={2}>
      {err && <div className="alert alert-danger" role="alert">{err}</div>}
      {info && !err && <div className="alert alert-info" role="status">{info}</div>}
      {analysis && (
        <div className="card-section" aria-label="Ostatnia analiza biometryczna">
          <p><strong>Model:</strong> {analysis.modelVersion}</p>
          <p><strong>Jakość:</strong> {analysis.qualityScore.toFixed(3)} | <strong>Liveness:</strong> {analysis.livenessScore.toFixed(3)}</p>
        </div>
      )}
      <div className="card-section">
        <p>
          {status?.enabled
            ? <>Biometria twarzy jest aktywna ({status.activeCount} próbka).</>
            : <>Brak zapisanej biometrii twarzy. Dodaj referencję, aby włączyć logowanie twarzą.</>}
        </p>
        <div className="actions-row inline">
          <Button variant="primary" onClick={() => setShowPermission(true)} disabled={busy}>
            {status?.enabled ? "Aktualizuj próbkę" : "Skonfiguruj biometrię"}
          </Button>
          {status?.enabled && (
            <Button variant="outline" onClick={disableBiometrics} disabled={busy}>Wyłącz biometrię</Button>
          )}
        </div>
      </div>
      {stream && (
        <div className="card-section camera-section">
          <div className="camera-preview" aria-label="Podgląd z kamery">
            <video ref={videoRef} autoPlay muted playsInline width={320} height={240} />
          </div>
          <p className="helper-text">Ustaw twarz w centrum kadru w dobrze oświetlonym miejscu.</p>
          <div className="actions-row inline">
            <Button variant="primary" onClick={captureAndEnroll} disabled={busy}>Zapisz próbkę</Button>
            <Button variant="outline" onClick={stopStream} disabled={busy}>Zamknij podgląd</Button>
          </div>
        </div>
      )}

      {showPermission && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal">
            <h3>Użyj kamery</h3>
            <p>Potrzebujemy dostępu do kamery, aby nagrać wzorzec biometrii twarzy. Obraz zostanie zapisany w formie bezpiecznego wektora.</p>
            <div className="modal-actions">
              <Button variant="primary" onClick={startCamera} disabled={busy}>Zezwól i rozpocznij</Button>
              <Button variant="outline" onClick={() => setShowPermission(false)} disabled={busy}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}
    </Card>
  );
};

export default BiometricComponent;
