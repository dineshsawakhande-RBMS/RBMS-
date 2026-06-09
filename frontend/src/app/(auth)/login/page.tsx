"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useFormik } from "formik";
import * as Yup from "yup";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";
import Stack from "@mui/material/Stack";
import apiClient from "@/lib/apiClient";
import { useAuthStore } from "@/store/authStore";
import type { LoginRequest, LoginResponse } from "@/types";

const validationSchema = Yup.object({
  email: Yup.string().email("Enter a valid email").required("Email is required"),
  password: Yup.string().min(8, "At least 8 characters").required("Password is required"),
});

export default function LoginPage() {
  const router = useRouter();
  const login = useAuthStore((s) => s.login);
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik<LoginRequest>({
    initialValues: { email: "", password: "" },
    validationSchema,
    onSubmit: async (values, helpers) => {
      setServerError(null);
      try {
        const { data } = await apiClient.post<LoginResponse>("/auth/login", values);
        login(
          {
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            expiresAt: data.expiresAt,
          },
          data.user,
        );
        router.push("/dashboard");
      } catch (err) {
        const message =
          err instanceof AxiosError
            ? (err.response?.data?.message ?? "Invalid credentials")
            : "Something went wrong. Please try again.";
        setServerError(message);
      } finally {
        helpers.setSubmitting(false);
      }
    },
  });

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        p: 2,
      }}
    >
      <Card sx={{ width: "100%", maxWidth: 420 }} elevation={3}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h2" component="h1" gutterBottom>
            Sign in
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Access your Retail Business Management workspace.
          </Typography>

          <form onSubmit={formik.handleSubmit} noValidate>
            <Stack spacing={2}>
              {serverError && <Alert severity="error">{serverError}</Alert>}

              <TextField
                fullWidth
                id="email"
                name="email"
                label="Email"
                type="email"
                autoComplete="username"
                value={formik.values.email}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                error={formik.touched.email && Boolean(formik.errors.email)}
                helperText={formik.touched.email && formik.errors.email}
              />

              <TextField
                fullWidth
                id="password"
                name="password"
                label="Password"
                type="password"
                autoComplete="current-password"
                value={formik.values.password}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                error={formik.touched.password && Boolean(formik.errors.password)}
                helperText={formik.touched.password && formik.errors.password}
              />

              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={formik.isSubmitting}
              >
                {formik.isSubmitting ? "Signing in…" : "Sign in"}
              </Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
