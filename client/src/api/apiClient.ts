const API_URL = import.meta.env.VITE_API_URL as string | undefined;

type ApiProblem = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  fieldErrors?: Record<string, string[]>;

  constructor(
    status: number,
    message: string,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<T> {
  if (!API_URL) {
    throw new Error("VITE_API_URL has not been configured.");
  }

  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;

  try {
    response = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers,
      credentials: "include",
    });
  } catch {
    throw new ApiError(
      0,
      "Unable to connect to the Knight School server.",
    );
  }

  if (!response.ok) {
    let problem: ApiProblem = {};

    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      // The server may return an empty response.
    }

    throw new ApiError(
      response.status,
      problem.detail ??
        problem.title ??
        `Request failed with status ${response.status}.`,
      problem.errors,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}