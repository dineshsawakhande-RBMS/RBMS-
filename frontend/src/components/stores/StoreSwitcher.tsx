"use client";

import { useEffect, useMemo } from "react";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import Box from "@mui/material/Box";
import StorefrontIcon from "@mui/icons-material/Storefront";
import { useStores } from "@/features/stores/hooks";
import { useStoreStore } from "@/store/storeStore";

export default function StoreSwitcher() {
  const { data: stores } = useStores();
  const activeStoreId = useStoreStore((s) => s.activeStoreId);
  const setActiveStoreId = useStoreStore((s) => s.setActiveStoreId);

  const active = useMemo(() => (stores ?? []).filter((s) => s.isActive), [stores]);

  // Default to the first store, or recover if the persisted one no longer exists.
  useEffect(() => {
    if (active.length === 0) return;
    if (!activeStoreId || !active.some((s) => s.id === activeStoreId)) {
      setActiveStoreId(active[0]!.id);
    }
  }, [active, activeStoreId, setActiveStoreId]);

  if (active.length <= 1) return null; // no switcher needed for a single store

  return (
    <Box sx={{ display: "flex", alignItems: "center", mr: 1 }}>
      <StorefrontIcon fontSize="small" sx={{ mr: 0.5, opacity: 0.9 }} />
      <Select
        size="small"
        variant="standard"
        disableUnderline
        value={activeStoreId ?? ""}
        onChange={(e) => setActiveStoreId(e.target.value)}
        sx={{ color: "inherit", fontWeight: 600, "& .MuiSvgIcon-root": { color: "inherit" }, maxWidth: { xs: 120, tablet: 200 } }}
      >
        {active.map((s) => (
          <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>
        ))}
      </Select>
    </Box>
  );
}
