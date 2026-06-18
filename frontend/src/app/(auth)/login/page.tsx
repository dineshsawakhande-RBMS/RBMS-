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
import type { AuthResult, LoginRequest } from "@/types";

const validationSchema = Yup.object({
  username: Yup.string().required("Username is required"),
  password: Yup.string().required("Password is required"),
});

export default function LoginPage() {
  const router = useRouter();
  const setSession = useAuthStore((s) => s.setSession);
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik<LoginRequest>({
    initialValues: { username: "", password: "" },
    validationSchema,
    onSubmit: async (values, helpers) => {
      setServerError(null);
      try {
        const { data } = await apiClient.post<AuthResult>("/auth/login", values);
        setSession(data);
        router.push("/dashboard");
      } catch (err) {
        const message =
          err instanceof AxiosError && err.response?.status === 403
            ? "Invalid username or password."
            : err instanceof AxiosError
              ? ((err.response?.data as { title?: string })?.title ?? "Login failed.")
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
            Retail Business Management System
          </Typography>

          <form onSubmit={formik.handleSubmit} noValidate>
            <Stack spacing={2}>
              {serverError && <Alert severity="error">{serverError}</Alert>}

              <TextField
                fullWidth
                id="username"
                name="username"
                label="Username"
                autoComplete="username"
                value={formik.values.username}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                error={formik.touched.username && Boolean(formik.errors.username)}
                helperText={formik.touched.username && formik.errors.username}
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

              <Button type="submit" variant="contained" size="large" disabled={formik.isSubmitting}>
                {formik.isSubmitting ? "Signing in…" : "Sign in"}
              </Button>

              <Alert severity="info" variant="outlined">
                Demo login — <strong>owner</strong> / <strong>Password123!</strong>
              </Alert>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
