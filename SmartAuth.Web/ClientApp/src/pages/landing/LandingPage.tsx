import React from "react";
import { jwtDecode } from "jwt-decode";
import { getJwt, logout } from "../../auth/AuthService";
import { useNavigate } from "react-router-dom";

type Claims = {
    sub: string;
    email?: string;
    name?: string;
    role?: string | string[];
    exp: number;
};

const LandingPage: React.FC = () => {
    const nav = useNavigate();
    const token = getJwt();

    let claims: Claims | null = null;
    try {
        claims = token ? (jwtDecode<Claims>(token) as Claims) : null;
    } catch {
        // token uszkodzony → wyloguj i wróć do loginu
        logout();
        nav("/login", { replace: true });
        return null;
    }

    // Normalizacja ról: zawsze tablica stringów
    const roles: string[] = Array.isArray(claims?.role)
        ? (claims!.role as string[])
        : claims?.role
            ? [String(claims.role)]
            : [];

    return (
        <div className="min-h-screen p-6">
            <div className="max-w-3xl mx-auto">
                <div className="flex items-center justify-between mb-6">
                    <h1 className="text-2xl font-semibold">
                        Witaj{claims?.name ? `, ${claims.name}` : ""}!
                    </h1>
                    <button
                        className="rounded-xl px-4 py-2 bg-gray-900 text-white"
                        onClick={() => {
                            logout();
                            nav("/login", { replace: true });
                        }}
                    >
                        Wyloguj
                    </button>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                    <div className="rounded-2xl border p-4">
                        <h2 className="font-medium">Twoje dane</h2>
                        <ul className="text-sm text-gray-700 mt-2">
                            <li>
                                <b>sub:</b> {claims?.sub ?? "-"}
                            </li>
                            <li>
                                <b>email:</b> {claims?.email ?? "-"}
                            </li>
                            <li>
                                <b>role:</b> {roles.length ? roles.join(", ") : "-"}
                            </li>
                            <li>
                                <b>expires:</b>{" "}
                                {claims ? new Date(claims.exp * 1000).toLocaleString() : "-"}
                            </li>
                        </ul>
                    </div>

                    <div className="rounded-2xl border p-4">
                        <h2 className="font-medium">Skróty</h2>
                        <div className="mt-2 text-sm text-gray-600">
                            Tu dodasz kafelki do modułów po zalogowaniu.
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default LandingPage;
