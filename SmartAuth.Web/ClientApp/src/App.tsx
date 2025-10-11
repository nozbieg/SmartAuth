import {BrowserRouter, Routes, Route, Navigate} from "react-router-dom";

import LoginPage from "./pages/login/LoginPage";
import RegisterPage from "./pages/register/RegisterPage";
import LandingPage from "./pages/landing/LandingPage";
import {RouteGuard} from "./auth/RouteGuard";

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Start → Login */}
                <Route path="/" element={<LoginPage/>}/>

                {/* Rejestracja (publiczna trasa) */}
                <Route path="/register" element={<RegisterPage/>}/>

                {/* Chronione trasy */}
                <Route element={<RouteGuard />}>
                    <Route path="/home" element={<LandingPage />} />
                </Route>


                {/* Fallback */}
                <Route path="*" element={<Navigate to="/" replace/>}/>
            </Routes>
        </BrowserRouter>
    );
}
