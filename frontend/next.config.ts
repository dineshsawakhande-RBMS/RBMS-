import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Produces a minimal standalone server bundle used by web.Dockerfile.
  output: "standalone",
  reactStrictMode: true,
  // MUI / emotion play nicer with the modern compiler defaults.
  experimental: {
    optimizePackageImports: ["@mui/material", "@mui/icons-material"],
  },
};

export default nextConfig;
