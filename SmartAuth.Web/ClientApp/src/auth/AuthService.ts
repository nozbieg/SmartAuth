﻿import {jwtDecode} from "jwt-decode";

type Claims = { exp?: number; [k: string]: unknown };

export type LoginResponse = {
    requires2Fa: boolean;
    token: string;
    methods?: Array<"code" | "totp">;
};

export type Verify2FAResponse = { jwt: string };
export type TotpSetupResponse = { setupId: string; secret: string; otpAuthUri: string; qrImageBase64: string };
export type TotpStatusResponse = { active: boolean };

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
