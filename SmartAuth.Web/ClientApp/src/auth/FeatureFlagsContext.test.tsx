import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FeatureFlagsProvider, useFeatureFlags } from './FeatureFlagsContext';

beforeEach(() => { vi.resetAllMocks(); });

describe('FeatureFlagsContext', () => {
  it('ładuje flagi i udostępnia je w hooku', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true, json: () => Promise.resolve({ twofa_code: true }) } as any);

    function Probe() {
      const { flags, loading } = useFeatureFlags();
      if (loading) return <div>Ładowanie...</div>;
      return <div>{flags?.twofa_code ? 'code-on' : 'code-off'}</div>;
    }

    render(<FeatureFlagsProvider><Probe /></FeatureFlagsProvider>);
    expect(screen.getByText(/Ładowanie/i)).toBeInTheDocument();
    await waitFor(() => screen.getByText(/code-on/));
    expect(fetchMock).toHaveBeenCalledWith('/api/feature-flags', { credentials: 'include' });
  });

  it('ustawia loading=false przy błędzie', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false, json: () => Promise.resolve({}) } as any);
    function Probe() {
      const { loading, flags } = useFeatureFlags();
      if (!loading) return <div>DONE {flags ? 'F' : 'NF'}</div>; return <div>Ładowanie...</div>;
    }
    render(<FeatureFlagsProvider><Probe /></FeatureFlagsProvider>);
    await waitFor(() => screen.getByText(/DONE NF/));
  });
});

