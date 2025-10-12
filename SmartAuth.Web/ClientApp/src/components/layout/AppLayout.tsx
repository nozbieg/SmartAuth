import React from "react";
import { logout } from "../../auth/AuthService";
import { useNavigate } from "react-router-dom";
import Footer from "./Footer";
import Button from "../ui/Button";

interface AppLayoutProps {
  title?: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
  footerNote?: React.ReactNode;
}

const AppLayout: React.FC<AppLayoutProps> = ({ title = "SmartAuth", actions, children, footerNote }) => {
  const nav = useNavigate();
  return (
    <div className="app-shell">
      <header className="app-header" role="banner">
        <div className="header-inner">
          <div className="brand" onClick={() => nav('/home')} role="link" tabIndex={0} onKeyDown={e => { if (e.key==='Enter') nav('/home'); }}>{title}</div>
          <div className="header-actions">
            {actions}
            <Button variant="outline" onClick={() => { logout(); nav('/login', { replace: true }); }}>Wyloguj</Button>
          </div>
        </div>
      </header>
      <main className="main-content" role="main">
        {children}
      </main>
      <Footer note={footerNote} />
    </div>
  );
};

export default AppLayout;
