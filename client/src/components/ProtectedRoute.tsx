import { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router";

import { ApiError } from "../api/apiClient";
import { getCurrentUser } from "../api/authApi";

type ProtectedRouteProps = {
  requiredRole?: string;
};

type AuthenticationState =
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "error";

function ProtectedRoute({ requiredRole }: ProtectedRouteProps) {
  const [authenticationState, setAuthenticationState] =
    useState<AuthenticationState>("loading");

  const [roles, setRoles] = useState<string[]>([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    let isActive = true;

    async function checkAuthentication() {
      setAuthenticationState("loading");
      setErrorMessage("");

      try {
        const user = await getCurrentUser();

        if (!isActive) {
          return;
        }

        setRoles(user.roles);
        setAuthenticationState("authenticated");
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          setAuthenticationState("unauthenticated");
          return;
        }

        if (error instanceof ApiError) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "Something unexpected happened while checking your session.",
          );
        }

        setAuthenticationState("error");
      }
    }

    void checkAuthentication();

    return () => {
      isActive = false;
    };
  }, [retryCount]);

  if (authenticationState === "loading") {
    return (
      <main>
        <p role="status">Checking your session...</p>
      </main>
    );
  }

  if (authenticationState === "unauthenticated") {
    return <Navigate to="/login" replace />;
  }

  if (authenticationState === "error") {
    return (
      <main>
        <h1>Unable to verify your account</h1>
        <p role="alert">{errorMessage}</p>

        <button
          type="button"
          onClick={() => setRetryCount((current) => current + 1)}
        >
          Try again
        </button>
      </main>
    );
  }

  if (requiredRole && !roles.includes(requiredRole)) {
    return <Navigate to="/profile" replace />;
  }

  return <Outlet />;
}

export default ProtectedRoute;