import React from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { getJwt, isJwtValid } from "./AuthService";

export const RouteGuard: React.FC = () => {
    const location = useLocation();
    const token = getJwt();
    const ok = isJwtValid(token);

    if (!ok) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }
    return <Outlet />;
};