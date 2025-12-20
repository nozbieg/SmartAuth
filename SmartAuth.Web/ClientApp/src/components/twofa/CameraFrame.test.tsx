import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import CameraFrame from './CameraFrame';

describe('CameraFrame', () => {
  it('renderuje podgląd kamery z nakładką', () => {
    const videoRef = React.createRef<HTMLVideoElement>();
    const { container } = render(<CameraFrame videoRef={videoRef} />);

    expect(container.querySelector('video')).toBeInTheDocument();
    expect(screen.getByText('Umieść twarz w okręgu')).toBeInTheDocument();
  });

  it('obsługuje suwak przybliżenia', () => {
    const onZoomChange = vi.fn();
    const videoRef = React.createRef<HTMLVideoElement>();

    render(
      <CameraFrame
        videoRef={videoRef}
        zoomRange={{ min: 1, max: 2, step: 0.1 }}
        zoom={1.5}
        onZoomChange={onZoomChange}
        zoomControlId="zoom"
      />
    );

    const slider = screen.getByRole('slider', { name: /Przybliżenie kamery/i });
    expect(screen.getByText('1.5x')).toBeInTheDocument();

    fireEvent.change(slider, { target: { value: '1.7' } });
    expect(onZoomChange).toHaveBeenCalledWith(1.7);
  });
});
