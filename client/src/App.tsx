import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
} from "react-router";

import ProtectedRoute from "./components/ProtectedRoute";
import AdminCreateUserPage from "./pages/AdminCreateUserPage";
import AdminDashboardPage from "./pages/AdminDashboardPage";
import AdminEditUserPage from "./pages/AdminEditUserPage";
import AdminProgressPage from "./pages/AdminProgressPage";
import AdminUsersPage from "./pages/AdminUsersPage";
import LoginPage from "./pages/LoginPage";
import ProfilePage from "./pages/ProfilePage";
import RegistrationPage from "./pages/RegistrationPage";
import SkillTreePage from "./pages/SkillTreePage";
import AdminSkillsPage from "./pages/AdminSkillsPage";
import AdminCreateSkillPage from "./pages/AdminCreateSkillPage";
import AdminCreateDisciplinePage from "./pages/AdminCreateDisciplinePage";
import AdminEditSkillPage from "./pages/AdminEditSkillPage";
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

          <Route
            path="/admin/users/new"
            element={<AdminCreateUserPage />}
          />

          <Route
            path="/admin/users"
            element={<AdminUsersPage />}
          />

          <Route
            path="/admin/users/:userId/edit"
            element={<AdminEditUserPage />}
          />

          <Route
            path="/admin/progress"
            element={<AdminProgressPage />}
          />

           <Route
            path="/admin/skills"
            element={<AdminSkillsPage />}
          />

          <Route
          path="/admin/skills/new"
          element={<AdminCreateSkillPage />}
          />

          <Route
          path="/admin/disciplines/new"
         element={<AdminCreateDisciplinePage />}
          />

          <Route
          path="/admin/skills/:skillId/edit"
          element={<AdminEditSkillPage />}
          />

        </Route>

        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;