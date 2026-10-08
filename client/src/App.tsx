import { BrowserRouter, Navigate, Route, Routes } from "react-router";

import ProtectedRoute from "./components/ProtectedRoute";
import AdminDashboardPage from "./pages/AdminDashboardPage";
import LoginPage from "./pages/LoginPage";
import ProfilePage from "./pages/ProfilePage";
import RegistrationPage from "./pages/RegistrationPage";
import SkillTreePage from "./pages/SkillTreePage";
import AdminCreateUserPage from "./pages/AdminCreateUserPage";
import AdminUsersPage from "./pages/AdminUsersPage";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />

        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegistrationPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/skill-tree" element={<SkillTreePage />} />
        </Route>

        <Route
          element={<ProtectedRoute requiredRole="ADMINISTRATOR" />}
        >
          <Route path="/admin" element={<AdminDashboardPage />} />

          <Route path="/admin/users/new"element={<AdminCreateUserPage />}/>
          <Route path="/admin/users" element={<AdminUsersPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;