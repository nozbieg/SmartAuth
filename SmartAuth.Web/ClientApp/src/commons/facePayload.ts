import type { MutableRefObject } from "react";

function toBase64(buffer: Uint8Array): string {
  let binary = "";
  const chunk = 0x8000;
  for (let i = 0; i < buffer.length; i += chunk) {
    const slice = buffer.subarray(i, i + chunk);
    binary += String.fromCharCode(...slice);
  }
  return btoa(binary);
}

export function encodeRgbPayload(imageData: ImageData): string {
  const { width, height, data } = imageData;
  const payload = new Uint8Array(8 + width * height * 3);
  const view = new DataView(payload.buffer);
  view.setInt32(0, width, true);
  view.setInt32(4, height, true);

  for (let i = 0, j = 8; i < data.length; i += 4, j += 3) {
    payload[j] = data[i];
    payload[j + 1] = data[i + 1];
    payload[j + 2] = data[i + 2];
  }

  return toBase64(payload);
}

export function ensureCanvas(ref: MutableRefObject<HTMLCanvasElement | null>): HTMLCanvasElement {
  if (!ref.current) {
    ref.current = document.createElement("canvas");
  }
  return ref.current;
}
