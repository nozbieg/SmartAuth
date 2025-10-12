import React from "react";
import Footer from "./Footer";

interface AuthLayoutProps {
  /** Panel główny (formularze) */
  children: React.ReactNode;
  /** Tytuł sekcji medialnej */
  mediaTitle?: string;
  /** Opis sekcji medialnej */
  mediaDescription?: string;
  /** Dodatkowy element (np. logo) u góry panelu medialnego */
  mediaHeader?: React.ReactNode;
  /** Dolna stopka w panelu medialnym */
  mediaFooter?: React.ReactNode;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({
  children,
  mediaTitle = "SmartAuth",
  mediaDescription = "Nowoczesna, modułowa platforma uwierzytelniania z obsługą wielu metod drugiego czynnika.",
  mediaHeader,
  mediaFooter,
}) => {
  return (
    <div className="app-shell">
      <main className="auth-wrapper">
        <div className="auth-grid" role="presentation">
          <section className="auth-media" aria-label="Informacje o aplikacji">
            <div>
              {mediaHeader}
              <h2>{mediaTitle}</h2>
              <p>{mediaDescription}</p>
            </div>
            <footer>{mediaFooter || <span>© {new Date().getFullYear()} SmartAuth</span>}</footer>
          </section>
          <section className="auth-panel" aria-label="Panel uwierzytelniania">
            {children}
          </section>
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default AuthLayout;
