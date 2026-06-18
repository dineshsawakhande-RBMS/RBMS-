export type Role =
  | "SuperAdmin"
  | "Owner"
  | "Manager"
  | "Cashier"
  | "InventoryStaff"
  | "Accountant";

export interface AuthUser {
  userId: string;
  username: string;
  fullName: string;
  roles: Role[];
}

/** Matches the API's AuthResultDto (flat shape returned by /auth/login and /auth/refresh). */
export interface AuthResult extends AuthUser {
  accessToken: string;
  refreshToken: string;
  /** ISO-8601 timestamp when the access token expires. */
  accessTokenExpiresAt: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}
