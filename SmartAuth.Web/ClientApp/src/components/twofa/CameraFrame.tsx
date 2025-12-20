import React, { useId } from "react";

export type ZoomRange = { min: number; max: number; step: number };

export interface CameraFrameProps {
  videoRef: React.RefObject<HTMLVideoElement>;
  overlayText?: string;
  zoomRange?: ZoomRange | null;
  zoom?: number | null;
  onZoomChange?: (value: number) => void;
  zoomControlId?: string;
}

const CameraFrame: React.FC<CameraFrameProps> = ({
  videoRef,
  overlayText = "Umieść twarz w okręgu",
  zoomRange,
  zoom,
  onZoomChange,
  zoomControlId
}) => {
  const generatedId = useId();
  const controlId = zoomControlId ?? generatedId;

  return (
    <>
      <div className="camera-preview" aria-label="Podgląd z kamery">
        <div className="camera-frame">
          <video ref={videoRef} autoPlay muted playsInline width={320} height={240} />
          <div className="face-overlay" aria-hidden="true">
            <div className="face-circle" />
            <span className="face-overlay-text">{overlayText}</span>
          </div>
        </div>
      </div>
      {zoomRange && onZoomChange && (
        <div className="zoom-control">
          <div className="zoom-header">
            <label htmlFor={controlId}>Przybliżenie kamery</label>
            {zoom !== null && <span className="zoom-value">{zoom.toFixed(1)}x</span>}
          </div>
          <input
            id={controlId}
            type="range"
            min={zoomRange.min}
            max={zoomRange.max}
            step={zoomRange.step}
            value={zoom ?? zoomRange.min}
            onChange={(e) => onZoomChange(Number(e.target.value))}
          />
          <div className="zoom-scale">
            <span>{zoomRange.min.toFixed(1)}x</span>
            <span>{zoomRange.max.toFixed(1)}x</span>
          </div>
        </div>
      )}
    </>
  );
};

export default CameraFrame;
