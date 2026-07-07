"use client";

import { useEffect, useMemo } from "react";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import Box from "@mui/material/Box";
import StorefrontIcon from "@mui/icons-material/Storefront";
import { useStores } from "@/features/stores/hooks";
import { useStoreStore } from "@/store/storeStore";
import { DEFAULT_STORE_ID } from "@/lib/config";

export default function StoreSwitcher() {
  const { data: stores } = useStores();
  const activeStoreId = useStoreStore((s) => s.activeStoreId);
  const setActiveStoreId = useStoreStore((s) => s.setActiveStoreId);

  const active = useMemo(() => (stores ?? []).filter((s) => s.isActive), [stores]);

  // Default to the primary (seeded) store when available, else the first; recover if the
  // persisted selection no longer exists.
  useEffect(() => {
    if (active.length === 0) return;
    if (!activeStoreId || !active.some((s) => s.id === activeStoreId)) {
      const preferred = active.find((s) => s.id === DEFAULT_STORE_ID) ?? active[0]!;
      setActiveStoreId(preferred.id);
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
