import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";

import { FeatureFlagsProvider } from "./auth/FeatureFlagsContext";
import { RouteGuard } from "./auth/RouteGuard"; 

import LoginPage from "./pages/login/LoginPage";
import RegisterPage from "./pages/register/RegisterPage";
import LandingPage from "./pages/landing/LandingPage";

import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <FeatureFlagsProvider>
            <BrowserRouter>
                <Routes>
                    {/* PUBLIC */}
                    <Route path="/login" element={<LoginPage />} />
                    <Route path="/register" element={<RegisterPage />} />
                    <Route path="/" element={<Navigate to="/login" replace />} />

                    {/* PROTECTED (Outlet) */}
                    <Route element={<RouteGuard />}>
                        <Route path="/home" element={<LandingPage />} />
                    </Route>

                    {/* FALLBACK */}
                    <Route path="*" element={<Navigate to="/login" replace />} />
                </Routes>
            </BrowserRouter>
        </FeatureFlagsProvider>
    </React.StrictMode>
);
