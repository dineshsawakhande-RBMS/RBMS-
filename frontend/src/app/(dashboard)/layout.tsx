"use client";

import { useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import Box from "@mui/material/Box";
import AppBar from "@mui/material/AppBar";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Avatar from "@mui/material/Avatar";
import BottomNavigation from "@mui/material/BottomNavigation";
import BottomNavigationAction from "@mui/material/BottomNavigationAction";
import Paper from "@mui/material/Paper";
import MenuIcon from "@mui/icons-material/Menu";
import DashboardIcon from "@mui/icons-material/Dashboard";
import Inventory2Icon from "@mui/icons-material/Inventory2";
import CategoryIcon from "@mui/icons-material/Category";
import LocalShippingIcon from "@mui/icons-material/LocalShipping";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import PointOfSaleIcon from "@mui/icons-material/PointOfSale";
import AssessmentIcon from "@mui/icons-material/Assessment";
import LogoutIcon from "@mui/icons-material/Logout";
import Brightness4Icon from "@mui/icons-material/Brightness4";
import Brightness7Icon from "@mui/icons-material/Brightness7";
import { useColorMode } from "@/components/providers/AppProviders";
import { useAuthStore } from "@/store/authStore";
import RouteGuard from "@/components/auth/RouteGuard";
import SessionManager from "@/components/auth/SessionManager";

const DRAWER_WIDTH = 248;

const navItems = [
  { label: "Dashboard", href: "/dashboard", icon: <DashboardIcon /> },
  { label: "Products", href: "/products", icon: <CategoryIcon /> },
  { label: "Inventory", href: "/inventory", icon: <Inventory2Icon /> },
  { label: "Sales", href: "/sales", icon: <PointOfSaleIcon /> },
  { label: "Suppliers", href: "/suppliers", icon: <LocalShippingIcon /> },
  { label: "Purchases", href: "/purchases", icon: <ReceiptLongIcon /> },
  { label: "Reports", href: "/reports", icon: <AssessmentIcon /> },
];

// Most-used destinations for the mobile bottom bar.
const bottomItems = ["/dashboard", "/sales", "/inventory", "/products"]
  .map((href) => navItems.find((n) => n.href === href))
  .filter((n): n is (typeof navItems)[number] => Boolean(n));

function initials(name?: string | null) {
  if (!name) return "U";
  return name.split(" ").map((w) => w[0]).slice(0, 2).join("").toUpperCase();
}

export default function DashboardLayout({ children }: { children: ReactNode }) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const pathname = usePathname();
  const router = useRouter();
  const { mode, toggleColorMode } = useColorMode();
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);

  const go = (href: string) => {
    router.push(href);
    setMobileOpen(false);
  };

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  const isActive = (href: string) => pathname === href || pathname.startsWith(`${href}/`);

  const drawer = (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <Toolbar sx={{ px: 2 }}>
        <Typography
          variant="h5"
          sx={{
            fontWeight: 800,
            letterSpacing: "-0.03em",
            background: "linear-gradient(135deg,#6C5CE7,#FF5C8A)",
            backgroundClip: "text",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
          }}
        >
          RBMS
        </Typography>
      </Toolbar>
      <List sx={{ flexGrow: 1, py: 1 }}>
        {navItems.map((item) => (
          <ListItem key={item.href} disablePadding>
            <ListItemButton selected={isActive(item.href)} onClick={() => go(item.href)}>
              <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} primaryTypographyProps={{ fontWeight: 600 }} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Box>
  );

  return (
    <RouteGuard>
      <SessionManager />
      <Box sx={{ display: "flex", minHeight: "100vh" }}>
        <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
          <Toolbar>
            <IconButton
              edge="start"
              onClick={() => setMobileOpen((v) => !v)}
              sx={{ mr: 1, display: { xs: "inline-flex", desktop: "none" } }}
              aria-label="open navigation"
            >
              <MenuIcon />
            </IconButton>
            <Typography variant="h6" noWrap sx={{ flexGrow: 1, fontWeight: 700 }}>
              Retail Business Management
            </Typography>

            <Tooltip title={mode === "dark" ? "Light mode" : "Dark mode"}>
              <IconButton onClick={toggleColorMode} aria-label="toggle theme">
                {mode === "dark" ? <Brightness7Icon /> : <Brightness4Icon />}
              </IconButton>
            </Tooltip>
            <Tooltip title={user?.fullName ?? "Account"}>
              <Avatar sx={{ width: 34, height: 34, ml: 1, bgcolor: "primary.main", fontSize: 14, fontWeight: 700 }}>
                {initials(user?.fullName)}
              </Avatar>
            </Tooltip>
            <Tooltip title="Sign out">
              <IconButton onClick={handleLogout} aria-label="sign out" sx={{ ml: 0.5 }}>
                <LogoutIcon />
              </IconButton>
            </Tooltip>
          </Toolbar>
        </AppBar>

        <Box component="nav" sx={{ width: { desktop: DRAWER_WIDTH }, flexShrink: { desktop: 0 } }}>
          {/* Drawer for tablet + mobile (below desktop) */}
          <Drawer
            variant="temporary"
            open={mobileOpen}
            onClose={() => setMobileOpen(false)}
            ModalProps={{ keepMounted: true }}
            sx={{
              display: { xs: "block", desktop: "none" },
              "& .MuiDrawer-paper": { boxSizing: "border-box", width: DRAWER_WIDTH },
            }}
          >
            {drawer}
          </Drawer>
          {/* Permanent sidebar for desktop (>= 1112px) */}
          <Drawer
            variant="permanent"
            open
            sx={{
              display: { xs: "none", desktop: "block" },
              "& .MuiDrawer-paper": {
                boxSizing: "border-box",
                width: DRAWER_WIDTH,
                borderRight: (t) => `1px solid ${t.palette.divider}`,
              },
            }}
          >
            {drawer}
          </Drawer>
        </Box>

        <Box
          component="main"
          sx={{
            flexGrow: 1,
            p: { xs: 2, tablet: 3 },
            pb: { xs: 10, tablet: 3 }, // room for the mobile bottom nav
            width: { desktop: `calc(100% - ${DRAWER_WIDTH}px)` },
          }}
        >
          <Toolbar />
          {/* Subtle page transition on navigation */}
          <Box
            key={pathname}
            sx={{
              animation: "rbmsFade .35s ease",
              "@keyframes rbmsFade": {
                from: { opacity: 0, transform: "translateY(10px)" },
                to: { opacity: 1, transform: "none" },
              },
            }}
          >
            {children}
          </Box>
        </Box>

        {/* Mobile-only bottom navigation (< 744px) */}
        <Paper
          elevation={8}
          sx={{
            position: "fixed",
            bottom: 0,
            left: 0,
            right: 0,
            display: { xs: "block", tablet: "none" },
            zIndex: (t) => t.zIndex.drawer + 2,
            borderTop: (t) => `1px solid ${t.palette.divider}`,
          }}
        >
          <BottomNavigation
            showLabels
            value={bottomItems.findIndex((i) => isActive(i.href))}
            onChange={(_, idx) => {
              const item = bottomItems[idx];
              if (item) go(item.href);
            }}
          >
            {bottomItems.map((i) => (
              <BottomNavigationAction key={i.href} label={i.label} icon={i.icon} />
            ))}
          </BottomNavigation>
        </Paper>
      </Box>
    </RouteGuard>
  );
}
