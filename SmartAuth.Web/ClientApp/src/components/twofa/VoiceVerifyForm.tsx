import React, { useEffect, useRef, useState } from "react";
import Button from "../ui/Button";
import { ApiError, verifyVoice } from "../../auth/AuthService";
import { blobToWavBase64 } from "../../commons/audioPayload";

export interface VoiceVerifyFormProps {
  tempToken: string;
  onSuccess: (jwt: string) => void;
  onCancel: () => void;
  disabled?: boolean;
  onBusyChange?: (busy: boolean) => void;
}

type RecorderState = "idle" | "recording" | "processing";

const VoiceVerifyForm: React.FC<VoiceVerifyFormProps> = ({ tempToken, onSuccess, onCancel, disabled, onBusyChange }) => {
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>("Nagraj krótką próbkę, aby potwierdzić tożsamość.");
  const [showPermission, setShowPermission] = useState(false);
  const [recorderState, setRecorderState] = useState<RecorderState>("idle");
  const [level, setLevel] = useState(0);
  const mediaRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<BlobPart[]>([]);
  const rafRef = useRef<number | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);

  useEffect(() => () => cleanup(), []);

  function setBusyState(next: boolean) {
    onBusyChange?.(next);
  }

  function cleanup() {
    recorderRef.current?.stop();
    mediaRef.current?.getTracks().forEach(t => t.stop());
    recorderRef.current = null;
    mediaRef.current = null;
    chunksRef.current = [];
    if (rafRef.current) cancelAnimationFrame(rafRef.current);
    analyserRef.current?.disconnect();
    analyserRef.current = null;
    setLevel(0);
  }

  function startMeter(stream: MediaStream) {
    try {
      const audioCtx = new AudioContext();
      const source = audioCtx.createMediaStreamSource(stream);
      const analyser = audioCtx.createAnalyser();
      analyser.fftSize = 256;
      source.connect(analyser);
      analyserRef.current = analyser;
      const data = new Uint8Array(analyser.frequencyBinCount);
      const loop = () => {
        analyser.getByteTimeDomainData(data);
        const rms = Math.sqrt(Array.from(data).reduce((acc, v) => acc + Math.pow((v - 128) / 128, 2), 0) / data.length);
        setLevel(rms);
        rafRef.current = requestAnimationFrame(loop);
      };
      loop();
    } catch {
      setLevel(0);
    }
  }

  async function startRecording() {
    if (!navigator.mediaDevices?.getUserMedia) {
      setErr("Twoja przeglądarka nie obsługuje nagrywania audio.");
      setShowPermission(false);
      return;
    }
    setErr(null);
    setInfo(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      mediaRef.current = stream;
      startMeter(stream);
      const recorder = new MediaRecorder(stream);
      recorderRef.current = recorder;
      chunksRef.current = [];
      recorder.ondataavailable = e => chunksRef.current.push(e.data);
      recorder.onstop = handleStopInternal;
      recorder.start();
      setRecorderState("recording");
    } catch (e: any) {
      setErr(e?.message || "Nie udało się uzyskać mikrofonu.");
      cleanup();
    } finally {
      setShowPermission(false);
    }
  }

  async function handleStopInternal() {
    if (chunksRef.current.length === 0) {
      cleanup();
      setRecorderState("idle");
      return;
    }
    setBusyState(true);
    setRecorderState("processing");
    try {
      const blob = new Blob(chunksRef.current, { type: "audio/webm" });
      const base64 = await blobToWavBase64(blob);
      const { jwt } = await verifyVoice(tempToken, base64);
      setInfo("Weryfikacja głosem zakończona.");
      cleanup();
      onSuccess(jwt);
    } catch (e: any) {
      if (e instanceof ApiError) setErr(e.message); else setErr(e?.message || "Nie udało się potwierdzić głosu.");
    } finally {
      setRecorderState("idle");
      setBusyState(false);
    }
  }

  function stopRecording() {
    if (recorderState !== "recording") return;
    recorderRef.current?.stop();
    mediaRef.current?.getTracks().forEach(t => t.stop());
  }

  const recording = recorderState === "recording";

  return (
    <div className="twofa-face-panel">
      {err && <div className="alert alert-danger" role="alert">{err}</div>}
      {info && !err && <div className="alert alert-info" role="status">{info}</div>}

      <div className="card-section">
        <p>Nagraj 3-5 sekund mowy w cichym miejscu, patrząc w stronę mikrofonu.</p>
        <div className="actions-row inline">
          <Button variant="primary" onClick={() => setShowPermission(true)} disabled={disabled || recording}>
            {recording ? "Nagrywanie..." : "Użyj mikrofonu"}
          </Button>
          <Button variant="outline" onClick={onCancel} disabled={disabled}>Wróć</Button>
        </div>
      </div>

      {recording && (
        <div className="card-section">
          <div className="voice-rec">
            <div className="voice-status">Nagrywanie próbki głosu...</div>
            <div className="voice-visualizer" aria-hidden="true">
              <div className="voice-bar" style={{ transform: `scaleX(${1 + level})` }} />
            </div>
            <div className="actions-row inline">
              <Button variant="primary" onClick={stopRecording} disabled={disabled}>Zatrzymaj</Button>
              <Button variant="outline" onClick={cleanup} disabled={disabled}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}

      {showPermission && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal">
            <h3>Użyj mikrofonu</h3>
            <p>Potrzebujemy krótkiego nagrania, aby potwierdzić Twój głos.</p>
            <div className="modal-actions">
              <Button variant="primary" onClick={startRecording} disabled={disabled}>Zezwól i nagraj</Button>
              <Button variant="outline" onClick={() => setShowPermission(false)} disabled={disabled}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default VoiceVerifyForm;
