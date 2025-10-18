import { describe, it, expect, vi } from 'vitest';
import { getFeatureFlags } from './featureFlags';

describe('getFeatureFlags', () => {
  it('pobiera poprawnie flagi', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true, json: () => Promise.resolve({ twofa_code: true }) } as any);
    const flags = await getFeatureFlags();
    expect(flags.twofa_code).toBe(true);
    expect(fetchMock).toHaveBeenCalledWith('/api/feature-flags', { credentials: 'include' });
  });
  it('rzuca gdy status !ok', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false, json: () => Promise.resolve({}) } as any);
    await expect(getFeatureFlags()).rejects.toThrow(/Cannot load feature flags/i);
  });
});

