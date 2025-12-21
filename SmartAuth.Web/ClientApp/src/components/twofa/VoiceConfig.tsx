import React, { useEffect, useRef, useState } from "react";
import Card from "../ui/Card";
import Button from "../ui/Button";
import { ApiError, voiceDisable, voiceEnroll, voiceStatus, type VoiceEnrollResponse, type VoiceStatusResponse } from "../../auth/AuthService";
import { blobToWavBase64 } from "../../commons/audioPayload";

type RecorderState = "idle" | "recording" | "processing";

const VoiceConfig: React.FC = () => {
  const jwt = localStorage.getItem("access_token");
  const [status, setStatus] = useState<VoiceStatusResponse | null>(null);
  const [analysis, setAnalysis] = useState<VoiceEnrollResponse | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [showPermission, setShowPermission] = useState(false);
  const [recorderState, setRecorderState] = useState<RecorderState>("idle");
  const [level, setLevel] = useState(0);
  const mediaRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<BlobPart[]>([]);
  const rafRef = useRef<number | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);

  useEffect(() => {
    if (!jwt) return;
    let active = true;
    (async () => {
      try {
        const current = await voiceStatus(jwt);
        if (active) setStatus(current);
      } catch (e: any) {
        if (active) setErr(e?.message || "Błąd pobierania statusu głosu");
      }
    })();
    return () => { active = false; };
  }, [jwt]);

  useEffect(() => () => cleanup(), []);

  if (!jwt) return null;

  async function refreshStatus() {
    const current = await voiceStatus(jwt);
    setStatus(current);
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
      const dataArray = new Uint8Array(analyser.frequencyBinCount);

      const tick = () => {
        analyser.getByteTimeDomainData(dataArray);
        let sum = 0;
        for (let i = 0; i < dataArray.length; i++) {
          const v = (dataArray[i] - 128) / 128;
          sum += v * v;
        }
        const rms = Math.sqrt(sum / dataArray.length);
        setLevel(rms);
        rafRef.current = requestAnimationFrame(tick);
      };
      tick();
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
    setAnalysis(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      mediaRef.current = stream;
      startMeter(stream);
      const recorder = new MediaRecorder(stream);
      recorderRef.current = recorder;
      chunksRef.current = [];
      recorder.ondataavailable = (e) => chunksRef.current.push(e.data);
      recorder.onstop = handleStopInternal;
      recorder.start();
      setRecorderState("recording");
    } catch (e: any) {
      setErr(e?.message || "Nie udało się uzyskać dostępu do mikrofonu.");
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
    setRecorderState("processing");
    try {
      const blob = new Blob(chunksRef.current, { type: "audio/webm" });
      const base64 = await blobToWavBase64(blob);
      const res = await voiceEnroll(jwt!, base64);
      setAnalysis(res);
      setInfo("Próbka głosu została zapisana.");
      await refreshStatus();
    } catch (e: any) {
      if (e instanceof ApiError) setErr(e.message); else setErr(e?.message || "Nie udało się zapisać próbki głosu.");
    } finally {
      cleanup();
      setRecorderState("idle");
    }
  }

  function stopRecording() {
    if (recorderState !== "recording") return;
    recorderRef.current?.stop();
    mediaRef.current?.getTracks().forEach(t => t.stop());
  }

  async function disableVoice() {
    if (!jwt) return;
    if (!window.confirm("Wyłączyć biometrię głosu?")) return;
    try {
      await voiceDisable(jwt);
      await refreshStatus();
      setInfo("Biometria głosu została wyłączona.");
      setAnalysis(null);
    } catch (e: any) {
      setErr(e?.message || "Nie udało się wyłączyć biometrii głosu");
    }
  }

  const recording = recorderState === "recording";

  return (
    <Card title="Biometria głosu" headingLevel={2}>
      {err && <div className="alert alert-danger" role="alert">{err}</div>}
      {info && !err && <div className="alert alert-info" role="status">{info}</div>}
      {analysis && (
        <div className="card-section" aria-label="Ostatnia analiza głosu">
          <p><strong>Model:</strong> {analysis.modelVersion}</p>
          <p><strong>Jakość:</strong> {analysis.qualityScore.toFixed(3)} | <strong>Czas:</strong> {analysis.durationSeconds.toFixed(2)}s</p>
        </div>
      )}
      <div className="card-section">
        <p>
          {status?.enabled
            ? <>Biometria głosu jest aktywna ({status.activeCount} próbka).</>
            : <>Brak zapisanej próbki głosu. Dodaj nagranie, aby włączyć logowanie głosem.</>}
        </p>
        <div className="actions-row inline">
          <Button variant="primary" onClick={() => setShowPermission(true)} disabled={recorderState !== "idle"}>
            {status?.enabled ? "Aktualizuj próbkę" : "Dodaj próbkę głosu"}
          </Button>
          {status?.enabled && (
            <Button variant="outline" onClick={disableVoice} disabled={recorderState !== "idle"}>Wyłącz głos</Button>
          )}
        </div>
      </div>

      {recording && (
        <div className="card-section">
          <div className="voice-rec">
            <div className="voice-status" aria-live="polite">Nagrywanie próbki... Powiedz zdanie w miarę głośno i wyraźnie.</div>
            <div className="voice-visualizer" aria-hidden="true">
              <div className="voice-bar" style={{ transform: `scaleX(${1 + level})` }} />
            </div>
            <div className="actions-row inline">
              <Button variant="primary" onClick={stopRecording}>Zatrzymaj nagranie</Button>
              <Button variant="outline" onClick={cleanup}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}

      {showPermission && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal">
            <h3>Użyj mikrofonu</h3>
            <p>Potrzebujemy dostępu do mikrofonu, aby nagrać próbkę głosu do logowania.</p>
            <div className="modal-actions">
              <Button variant="primary" onClick={startRecording} disabled={recorderState !== "idle"}>Zezwól i nagraj</Button>
              <Button variant="outline" onClick={() => setShowPermission(false)} disabled={recorderState !== "idle"}>Anuluj</Button>
            </div>
          </div>
        </div>
      )}
    </Card>
  );
};

export default VoiceConfig;
