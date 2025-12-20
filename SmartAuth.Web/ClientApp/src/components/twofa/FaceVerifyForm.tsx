import React, { useEffect, useRef, useState } from "react";
import Button from "../ui/Button";
import { ApiError, verifyFace } from "../../auth/AuthService";
import { encodeRgbPayload, ensureCanvas } from "../../commons/facePayload";
import CameraFrame, { type ZoomRange } from "./CameraFrame";

export interface FaceVerifyFormProps {
  tempToken: string;
  onSuccess: (jwt: string) => void;
  onCancel: () => void;
  disabled?: boolean;
  onBusyChange?: (busy: boolean) => void;
}

const FaceVerifyForm: React.FC<FaceVerifyFormProps> = ({ tempToken, onSuccess, onCancel, disabled, onBusyChange }) => {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>("Aby potwierdzić tożsamość, wykonamy krótką analizę twarzy.");
  const [showPermission, setShowPermission] = useState(false);
  const [zoomRange, setZoomRange] = useState<ZoomRange | null>(null);
  const [zoom, setZoom] = useState<number | null>(null);

  useEffect(() => {
    const video = videoRef.current;
    if (video && stream) {
      video.srcObject = stream;
      video.play().catch(() => undefined);
    }
    return () => { if (video) video.srcObject = null; };
  }, [stream]);

  useEffect(() => {
    if (!stream) {
      setZoomRange(null);
      setZoom(null);
      return;
    }

    const track = stream.getVideoTracks()[0];
    if (!track?.getCapabilities) {
      setZoomRange(null);
      setZoom(null);
      return;
    }

    const capabilities = track.getCapabilities();
    const zoomCap = (capabilities as MediaTrackCapabilities & { zoom?: MediaSettingsRange }).zoom;
    if (zoomCap && typeof zoomCap.min === "number" && typeof zoomCap.max === "number") {
      const step = zoomCap.step && zoomCap.step > 0 ? zoomCap.step : 0.1;
      const initialZoom = track.getSettings().zoom ?? zoomCap.min;
      setZoomRange({ min: zoomCap.min, max: zoomCap.max, step });
      setZoom(initialZoom);
      applyZoom(track, initialZoom);
    } else {
      setZoomRange(null);
      setZoom(null);
    }
  }, [stream]);

  useEffect(() => () => stopStream(), []);

  function setBusyState(next: boolean) {
    setBusy(next);
    onBusyChange?.(next);
  }

  async function requestCamera() {
    if (!navigator.mediaDevices?.getUserMedia) {
      setErr("Twoja przeglądarka nie udostępnia API kamery.");
      setShowPermission(false);
      return;
    }
    setErr(null);
    setInfo(null);
    setBusyState(true);
    try {
      const media = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" } });
      setStream(media);
    } catch (e: any) {
      setErr(e?.message || "Nie udało się uzyskać dostępu do kamery.");
    } finally {
      setShowPermission(false);
      setBusyState(false);
    }
  }

  function stopStream() {
    stream?.getTracks().forEach(t => t.stop());
    setStream(null);
    setZoomRange(null);
    setZoom(null);
  }

  function applyZoom(track: MediaStreamTrack, value: number) {
    track.applyConstraints({ advanced: [{ zoom: value }] }).catch(() => {
      setErr(prev => prev ?? "Nie udało się dostosować przybliżenia kamery.");
    });
  }

  function handleZoomChange(value: number) {
    setZoom(value);
    const track = stream?.getVideoTracks()[0];
    if (track) applyZoom(track, value);
  }

  async function captureAndVerify() {
    if (!tempToken || !videoRef.current || busy || disabled) return;
    setErr(null);
    setInfo(null);
    setBusyState(true);
    try {
      const video = videoRef.current;
      const canvas = ensureCanvas(canvasRef);
      const width = video.videoWidth || 640;
      const height = video.videoHeight || 480;
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext("2d", { willReadFrequently: true });
      if (!ctx) throw new Error("Brak dostępu do kontekstu wideo");

      ctx.drawImage(video, 0, 0, width, height);
      const payload = encodeRgbPayload(ctx.getImageData(0, 0, width, height));
      const { jwt } = await verifyFace(tempToken, payload);
      setInfo("Weryfikacja zakończyła się pomyślnie. Trwa logowanie...");
      stopStream();
      onSuccess(jwt);
    } catch (e: any) {
      if (e instanceof ApiError) setErr(e.message); else setErr(e?.message || "Nie udało się potwierdzić biometrii.");
    } finally {
      setBusyState(false);
    }
  }

  return (
    <div className="twofa-face-panel">
      {err && <div className="alert alert-danger" role="alert">{err}</div>}
      {info && !err && <div className="alert alert-info" role="status">{info}</div>}

      <div className="card-section">
        <p>Ustaw twarz w centrum kadru w dobrze oświetlonym miejscu. Proces potrwa kilka sekund.</p>
        <div className="actions-row inline">
          <Button variant="primary" onClick={() => setShowPermission(true)} disabled={busy || disabled}>
            {stream ? "Ponownie włącz kamerę" : "Użyj kamery"}
          </Button>
          <Button variant="outline" onClick={onCancel} disabled={busy}>Wróć</Button>
        </div>
      </div>

      {stream && (
        <div className="card-section camera-section">
          <CameraFrame
            videoRef={videoRef}
            zoomRange={zoomRange}
            zoom={zoom}
            onZoomChange={handleZoomChange}
          />
          <p className="helper-text">Pozostań nieruchomo i patrz w kamerę, aby dokończyć weryfikację.</p>
          <div className="actions-row inline">
            <Button variant="primary" onClick={captureAndVerify} disabled={busy || disabled}>Potwierdź twarzą</Button>
            <Button variant="outline" onClick={stopStream} disabled={busy}>Zamknij podgląd</Button>
          </div>
        </div>
      )}

      {showPermission && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal">
            <h3>Użyj kamery</h3>
            <p>Potrzebujemy dostępu do kamery, aby potwierdzić Twoją tożsamość za pomocą biometrii twarzy.</p>
            <div className="modal-actions">
              <Button variant="primary" onClick={requestCamera} disabled={busy}>Zezwól i rozpocznij</Button>
              <Button variant="outline" onClick={() => setShowPermission(false)} disabled={busy}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FaceVerifyForm;
