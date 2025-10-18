export type FeatureFlags = {
    twofa_code: boolean;
};

export async function getFeatureFlags(): Promise<FeatureFlags> {
    const res = await fetch("/api/feature-flags", {credentials: "include"});
    if (!res.ok) throw new Error("Cannot load feature flags");
    return (await res.json()) as FeatureFlags;
}