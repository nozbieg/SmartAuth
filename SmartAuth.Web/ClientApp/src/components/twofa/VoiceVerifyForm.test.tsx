import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import VoiceVerifyForm from './VoiceVerifyForm';
import * as AuthService from '../../auth/AuthService';
import * as AudioPayload from '../../commons/audioPayload';

vi.mock('../../auth/AuthService', () => ({
  verifyVoice: vi.fn(),
  ApiError: class ApiError extends Error {},
}));

vi.mock('../../commons/audioPayload', () => ({
  blobToWavBase64: vi.fn(),
}));

const verifyVoiceMock = AuthService.verifyVoice as unknown as ReturnType<typeof vi.fn>;
const blobToWavBase64Mock = AudioPayload.blobToWavBase64 as unknown as ReturnType<typeof vi.fn>;

class FakeAnalyser {
  public fftSize = 256;
  public frequencyBinCount = 8;
  getByteTimeDomainData(arr: Uint8Array) {
    for (let i = 0; i < arr.length; i++) arr[i] = 128;
  }
  disconnect() { /* noop */ }
}

class FakeAudioContext {
  analyser = new FakeAnalyser();
  createMediaStreamSource() { return { connect: vi.fn() }; }
  createAnalyser() { return this.analyser; }
  close() { return Promise.resolve(); }
}

class FakeMediaRecorder {
  public ondataavailable: ((ev: any) => void) | null = null;
  public onstop: (() => void) | null = null;
  constructor(public stream: MediaStream) {}
  start() {
    this.ondataavailable?.({ data: new Blob(['voice']) });
  }
  stop() {
    this.onstop?.();
  }
}

const fakeStream = { getTracks: () => [{ stop: vi.fn() }] } as unknown as MediaStream;

describe('VoiceVerifyForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (global as any).MediaRecorder = FakeMediaRecorder as any;
    (global as any).navigator = {
      mediaDevices: { getUserMedia: vi.fn().mockResolvedValue(fakeStream) }
    } as any;
    (global as any).AudioContext = FakeAudioContext as any;
    blobToWavBase64Mock.mockResolvedValue('data:audio/wav;base64,AAA');
    verifyVoiceMock.mockResolvedValue({ jwt: 'jwt-voice' });
  });

  it('nagrywa i weryfikuje próbkę głosu', async () => {
    const onSuccess = vi.fn();
    render(<VoiceVerifyForm tempToken="temp" onSuccess={onSuccess} onCancel={() => undefined} />);

    fireEvent.click(screen.getByRole('button', { name: /Użyj mikrofonu/i }));
    fireEvent.click(await screen.findByRole('button', { name: /Zezwól i nagraj/i }));

    await screen.findByText(/Nagrywanie próbki głosu/i);
    fireEvent.click(screen.getByRole('button', { name: /Zatrzymaj/i }));

    await waitFor(() => expect(verifyVoiceMock).toHaveBeenCalled());
    expect(onSuccess).toHaveBeenCalledWith('jwt-voice');
  });
});
