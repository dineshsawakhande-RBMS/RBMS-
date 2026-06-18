export * from "./auth";
export * from "./dashboard";
export * from "./api";

export interface ApiError {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
