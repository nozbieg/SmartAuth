import React from "react";
import { jwtDecode } from "jwt-decode";
import { getJwt, logout } from "../../auth/AuthService";
import { useNavigate } from "react-router-dom";
import AppLayout from "../../components/layout/AppLayout";
import Card from "../../components/ui/Card";

interface Claims {
    sub: string;
    email?: string;
    name?: string;
    role?: string | string[];
    exp: number;
}

const LandingPage: React.FC = () => {
    const nav = useNavigate();
    const token = getJwt();

    let claims: Claims | null = null;
    try {
        claims = token ? (jwtDecode<Claims>(token) as Claims) : null;
    } catch {
        logout();
        nav("/login", { replace: true });
        return null;
    }

    const roles: string[] = Array.isArray(claims?.role)
        ? (claims!.role as string[])
        : claims?.role
            ? [String(claims.role)]
            : [];

    return (
        <AppLayout title={claims?.name ? `Witaj, ${claims.name}` : 'Panel'}>
            <div className="cards" aria-label="Informacje użytkownika">
                <Card title="Twoje dane" headingLevel={2} aria-labelledby="card-dane">
                    <ul className="meta-list">
                        <li><span className="meta-label">sub:</span><span>{claims?.sub ?? '-'}</span></li>
                        <li><span className="meta-label">email:</span><span>{claims?.email ?? '-'}</span></li>
                        <li><span className="meta-label">role:</span><span>{roles.length ? roles.join(', ') : '-'}</span></li>
                        <li><span className="meta-label">wygasa:</span><span>{claims ? new Date(claims.exp * 1000).toLocaleString() : '-'}</span></li>
                    </ul>
                </Card>
                <Card title="Skróty" headingLevel={2}>
                    <p>Tu w przyszłości pojawią się kafelki do modułów systemu, konfiguracji 2FA oraz analityki bezpieczeństwa.</p>
                </Card>
            </div>
        </AppLayout>
    );
};

export default LandingPage;
