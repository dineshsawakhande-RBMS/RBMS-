"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import IconButton from "@mui/material/IconButton";
import Badge from "@mui/material/Badge";
import Tooltip from "@mui/material/Tooltip";
import Popover from "@mui/material/Popover";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import NotificationsIcon from "@mui/icons-material/Notifications";
import type { NotificationItem, NotificationSeverity } from "@/types";
import {
  useUnreadNotificationCount, useNotifications, useRefreshNotifications,
  useMarkNotificationRead, useMarkAllNotificationsRead,
} from "@/features/notifications/hooks";

const severityColor = (s: NotificationSeverity): "info" | "warning" | "error" =>
  s === "Critical" ? "error" : s === "Warning" ? "warning" : "info";

export default function NotificationBell() {
  const router = useRouter();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const open = Boolean(anchor);

  const { data: count = 0 } = useUnreadNotificationCount();
  const { data, isFetching } = useNotifications(open);
  const refresh = useRefreshNotifications();
  const markRead = useMarkNotificationRead();
  const markAll = useMarkAllNotificationsRead();

  const handleOpen = (e: React.MouseEvent<HTMLElement>) => {
    setAnchor(e.currentTarget);
    refresh.mutate();   // reconcile against live feeds when opened
  };

  const handleClick = (n: NotificationItem) => {
    if (!n.isRead) markRead.mutate(n.id);
    setAnchor(null);
    if (n.linkPath) router.push(n.linkPath);
  };

  const items = data?.items ?? [];

  return (
    <>
      <Tooltip title="Notifications">
        <IconButton onClick={handleOpen} aria-label="notifications">
          <Badge badgeContent={count} color="error" max={99}>
            <NotificationsIcon />
          </Badge>
        </IconButton>
      </Tooltip>

      <Popover
        open={open}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        slotProps={{ paper: { sx: { width: { xs: 320, tablet: 400 }, maxHeight: 480 } } }}
      >
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ px: 2, py: 1.5 }}>
          <Typography variant="subtitle1" fontWeight={700}>Notifications</Typography>
          <Button size="small" disabled={!items.some((n) => !n.isRead) || markAll.isPending} onClick={() => markAll.mutate()}>
            Mark all read
          </Button>
        </Stack>
        <Divider />
        {(isFetching || refresh.isPending) && (
          <Box sx={{ display: "flex", justifyContent: "center", py: 1 }}><CircularProgress size={20} /></Box>
        )}
        {items.length === 0 && !isFetching ? (
          <Box sx={{ py: 5, textAlign: "center", color: "text.secondary" }}>
            <Typography variant="body2">You&apos;re all caught up 🎉</Typography>
          </Box>
        ) : (
          <List dense disablePadding>
            {items.map((n) => (
              <ListItemButton
                key={n.id}
                onClick={() => handleClick(n)}
                sx={{ bgcolor: n.isRead ? "transparent" : "action.hover", alignItems: "flex-start", py: 1.25 }}
              >
                <ListItemText
                  primary={
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Chip size="small" color={severityColor(n.severity)} label={n.title} />
                      {!n.isRead && <Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: "primary.main" }} />}
                    </Stack>
                  }
                  secondary={n.message}
                  secondaryTypographyProps={{ sx: { mt: 0.5 } }}
                />
              </ListItemButton>
            ))}
          </List>
        )}
      </Popover>
    </>
  );
}
