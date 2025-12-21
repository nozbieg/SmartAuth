import {jwtDecode} from "jwt-decode";

type Claims = { exp?: number; [k: string]: unknown };

export type LoginResponse = {
    requires2Fa: boolean;
    token: string;
    methods?: Array<"code" | "totp" | "face" | "voice">;
};

export type Verify2FAResponse = { jwt: string };
export type TotpSetupResponse = { setupId: string; secret: string; otpAuthUri: string; qrImageBase64: string };
export type TotpStatusResponse = { active: boolean };
export type FaceStatusResponse = { enabled: boolean; activeCount: number };
export type FaceEnrollResponse = { biometricId: string; qualityScore: number; livenessScore: number; modelVersion: string };
export type VoiceStatusResponse = { enabled: boolean; activeCount: number };
export type VoiceEnrollResponse = { biometricId: string; qualityScore: number; durationSeconds: number; modelVersion: string };

export class ApiError extends Error {
    code?: string;
    status?: number;
    metadata?: Record<string, string> | null;
    traceId?: string;

    constructor(message: string, opts?: {
        code?: string;
        status?: number;
        metadata?: Record<string, string>;
        traceId?: string
    }) {
        super(message);
        this.name = 'ApiError';
        this.code = opts?.code;
        this.status = opts?.status;
        this.metadata = opts?.metadata ?? null;
        this.traceId = opts?.traceId;
    }
}

async function apiPost<T>(url: string, body: any, headers?: Record<string, string>): Promise<T> {
    let res: Response;
    try {
        res = await fetch(url, {
            method: 'POST',
            headers: {'Content-Type': 'application/json', ...(headers || {})},
            body: JSON.stringify(body)
        });
    } catch (e: any) {
        throw new ApiError(e?.message || 'Brak połączenia z serwerem');
    }
    let json: any = null;
    try {
        json = await res.json();
    } catch { /* brak body lub nie-json */
    }

    if (!res.ok) {
        const message = json?.message || json?.Message || `Żądanie nie powiodło się (${res.status})`;
        const code = json?.code || json?.Code;
        const status = json?.status || json?.Status || res.status;
        const rawMeta = json?.metadata || json?.Metadata;
        let metadata: Record<string, string> | undefined;
        if (rawMeta && typeof rawMeta === 'object') {
            metadata = {};
            for (const [k, v] of Object.entries(rawMeta)) {
                metadata[k] = typeof v === 'string' ? v : JSON.stringify(v);
            }
        }
        const traceId = json?.traceId || json?.TraceId;
        throw new ApiError(message, {code, status, metadata, traceId});
    }
    return json as T;
}

async function apiDelete<T>(url: string, headers?: Record<string, string>): Promise<T> {
    let res: Response;
    try {
        res = await fetch(url, { method: 'DELETE', headers: { ...(headers || {}) } });
    } catch (e: any) {
        throw new ApiError(e?.message || 'Brak połączenia z serwerem');
    }
    let json: any = null;
    try {
        json = await res.json();
    } catch { /* brak body */ }
    if (!res.ok) {
        const message = json?.message || json?.Message || `Żądanie nie powiodło się (${res.status})`;
        const code = json?.code || json?.Code;
        const status = json?.status || json?.Status || res.status;
        const rawMeta = json?.metadata || json?.Metadata;
        let metadata: Record<string, string> | undefined;
        if (rawMeta && typeof rawMeta === 'object') {
            metadata = {};
            for (const [k, v] of Object.entries(rawMeta)) metadata[k] = typeof v === 'string' ? v : JSON.stringify(v);
        }
        const traceId = json?.traceId || json?.TraceId;
        throw new ApiError(message, { code, status, metadata, traceId });
    }
    return json as T;
}

async function apiGet<T>(url: string, headers?: Record<string, string>): Promise<T> {
    let res: Response;
    try {
        res = await fetch(url, {method: 'GET', headers: {...(headers || {})}});
    } catch (e: any) {
        throw new ApiError(e?.message || 'Brak połączenia z serwerem');
    }
    let json: any = null;
    try {
        json = await res.json();
    } catch {
    }
    if (!res.ok) {
        const message = json?.message || json?.Message || `Żądanie nie powiodło się (${res.status})`;
        const code = json?.code || json?.Code;
        const status = json?.status || json?.Status || res.status;
        const rawMeta = json?.metadata || json?.Metadata;
        let metadata: Record<string, string> | undefined;
        if (rawMeta && typeof rawMeta === 'object') {
            metadata = {};
            for (const [k, v] of Object.entries(rawMeta)) metadata[k] = typeof v === 'string' ? v : JSON.stringify(v);
        }
        const traceId = json?.traceId || json?.TraceId;
        throw new ApiError(message, {code, status, metadata, traceId});
    }
    return json as T;
}

export async function loginWithPassword(email: string, password: string): Promise<LoginResponse> {
    return apiPost<LoginResponse>("/api/auth/login", {email, password});
}

export async function verifyCode(tempToken: string, code: string): Promise<Verify2FAResponse> {
    return apiPost<Verify2FAResponse>("/api/auth/2fa/code/verify", {code}, {Authorization: `Bearer ${tempToken}`});
}

export async function verifyFace(tempToken: string, imageBase64: string): Promise<Verify2FAResponse> {
    return apiPost<Verify2FAResponse>("/api/auth/2fa/face/verify", { imageBase64 }, { Authorization: `Bearer ${tempToken}` });
}

export async function verifyVoice(tempToken: string, audioBase64: string): Promise<Verify2FAResponse> {
    return apiPost<Verify2FAResponse>("/api/auth/2fa/voice/verify", { audioBase64 }, { Authorization: `Bearer ${tempToken}` });
}

export function saveJwt(jwt: string) {
    localStorage.setItem("access_token", jwt);
}

export function getJwt(): string | null {
    return localStorage.getItem("access_token");
}


export function logout() {
    localStorage.removeItem("access_token");
}

export function isJwtValid(token: string | null): boolean {
    if (!token) return false;
    try {
        const {exp} = jwtDecode<Claims>(token) || {};
        if (!exp) return false;
        const now = Math.floor(Date.now() / 1000);
        return exp > now;
    } catch (e) {
        return false;
    }
}

export async function totpStatus(jwt: string): Promise<TotpStatusResponse> {
    const raw = await apiGet<TotpStatusResponse>("/api/auth/2fa/totp/status", { Authorization: `Bearer ${jwt}` });
    return { active: raw.active };
}

export async function totpSetup(jwt: string, forceRestart: boolean = false): Promise<TotpSetupResponse> {
    return apiPost<TotpSetupResponse>("/api/auth/2fa/totp/setup", {forceRestart}, {Authorization: `Bearer ${jwt}`});
}

export async function totpEnable(jwt: string, setupId: string, code: string): Promise<{ message: string }> {
    return apiPost<{ message: string }>("/api/auth/2fa/totp/enable", {setupId, code}, {Authorization: `Bearer ${jwt}`});
}

export async function totpDisable(jwt: string): Promise<{ message: string }> {
    return apiPost<{ message: string }>("/api/auth/2fa/totp/disable", {}, {Authorization: `Bearer ${jwt}`});
}

export async function faceStatus(jwt: string): Promise<FaceStatusResponse> {
    return apiGet<FaceStatusResponse>("/api/auth/2fa/face/status", { Authorization: `Bearer ${jwt}` });
}

export async function faceEnroll(jwt: string, imageBase64: string): Promise<FaceEnrollResponse> {
    return apiPost<FaceEnrollResponse>("/api/auth/2fa/face/enroll", { imageBase64 }, { Authorization: `Bearer ${jwt}` });
}

export async function faceDisable(jwt: string): Promise<{ message?: string }> {
    return apiDelete<{ message?: string }>("/api/auth/2fa/face", { Authorization: `Bearer ${jwt}` });
}

export async function voiceStatus(jwt: string): Promise<VoiceStatusResponse> {
    return apiGet<VoiceStatusResponse>("/api/auth/2fa/voice/status", { Authorization: `Bearer ${jwt}` });
}

export async function voiceEnroll(jwt: string, audioBase64: string): Promise<VoiceEnrollResponse> {
    return apiPost<VoiceEnrollResponse>("/api/auth/2fa/voice/enroll", { audioBase64 }, { Authorization: `Bearer ${jwt}` });
}

export async function voiceDisable(jwt: string): Promise<{ message?: string }> {
    return apiDelete<{ message?: string }>("/api/auth/2fa/voice", { Authorization: `Bearer ${jwt}` });
}
