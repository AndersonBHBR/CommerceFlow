export type TokenResponse = {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  expiresAtUtc: string;
};

export type CurrentUser = {
  subject: string;
  name: string;
  roles: string[];
};

export type ServiceHealth = {
  key: "gateway" | "identity" | "sales" | "inventory";
  name: string;
  description: string;
  status: "operational" | "unavailable";
  latencyMs: number | null;
};

export type ApiProblem = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
};
