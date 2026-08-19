import { ref, watchEffect } from "vue";

const KEY = "azrael-color-mode";

function initialMode() {
  try {
    const saved = localStorage.getItem(KEY);
    if (saved === "light" || saved === "dark") return saved;
  } catch { /* ignore */ }
  return window.matchMedia?.("(prefers-color-scheme: light)").matches ? "light" : "dark";
}

const mode = ref(initialMode());

watchEffect(() => {
  document.documentElement.setAttribute("data-color-mode", mode.value);
  try {
    localStorage.setItem(KEY, mode.value);
  } catch { /* ignore */ }
});

export function useTheme() {
  const toggle = () => {
    mode.value = mode.value === "dark" ? "light" : "dark";
  };
  return { mode, toggle };
}
