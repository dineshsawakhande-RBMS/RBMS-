export type Role =
  | "Owner"
  | "Admin"
  | "Manager"
  | "Cashier"
  | "InventoryClerk"
  | "Accountant";

export interface User {
  id: string;
  email: string;
  fullName: string;
  roles: Role[];
  businessId: string;
  avatarUrl?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  /** Unix epoch seconds when the access token expires. */
  expiresAt: number;
}

export interface LoginResponse extends AuthTokens {
  user: User;
}
