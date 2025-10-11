import {jwtDecode} from "jwt-decode";

type Claims = { exp?: number; [k: string]: unknown };

export type LoginResponse = {
    requires2FA: boolean;
    methods?: Array<"code" | "face" | "voice">;
    tempToken?: string; // short-lived token to authorize 2FA verification calls
    jwt?: string; // when 2FA not required
};

export type Verify2FAResponse = {
    jwt: string; // final access token
};

export async function loginWithPassword(email: string, password: string): Promise<LoginResponse> {
    const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({email, password}),
    });
    if (!res.ok) throw new Error("Login failed");
    return res.json();
}


export async function verifyCode(tempToken: string, code: string): Promise<Verify2FAResponse> {
    const res = await fetch("/api/auth/2fa/code/verify", {
        method: "POST",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${tempToken}`},
        body: JSON.stringify({code}),
    });
    if (!res.ok) throw new Error("Invalid code");
    return res.json();
}


export async function verifyFace(tempToken: string, image: Blob): Promise<Verify2FAResponse> {
    const fd = new FormData();
    fd.append("image", image);
    const res = await fetch("/api/auth/2fa/face/verify", {
        method: "POST",
        headers: {Authorization: `Bearer ${tempToken}`},
        body: fd,
    });
    if (!res.ok) throw new Error("Face verification failed");
    return res.json();
}


export async function verifyVoice(tempToken: string, audio: Blob): Promise<Verify2FAResponse> {
    const fd = new FormData();
    fd.append("audio", audio);
    const res = await fetch("/api/auth/2fa/voice/verify", {
        method: "POST",
        headers: {Authorization: `Bearer ${tempToken}`},
        body: fd,
    });
    if (!res.ok) throw new Error("Voice verification failed");
    return res.json();
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