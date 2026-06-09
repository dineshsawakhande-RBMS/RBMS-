export * from "./auth";
export * from "./dashboard";

export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, string[]>;
}
