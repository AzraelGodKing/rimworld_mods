/** Resolve a compatibility entry to a Workshop URL or one of our mod pages. */
export function compatTarget(entry, mods) {
  if (!entry) return null;
  if (entry.url) return { external: true, href: entry.url };
  const n = String(entry.name || "").toLowerCase();
  const ours =
    mods.find((m) => m.name.toLowerCase() === n || m.id === n) ||
    (entry.id && mods.find((m) => m.id === entry.id));
  if (ours) return { external: false, href: `/${ours.id}` };
  return null;
}
