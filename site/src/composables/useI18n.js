import { ref, computed } from "vue";
import messages from "../data/i18n.json";

const KEY = "azrael-site-lang";
export const LANGS = [
  { id: "en", label: "EN" },
  { id: "ru", label: "РУ" },
  { id: "zh", label: "中文" },
];

function initialLang() {
  try {
    const saved = localStorage.getItem(KEY);
    if (saved && messages[saved]) return saved;
  } catch { /* ignore */ }
  const nav = (navigator.language || "en").toLowerCase();
  if (nav.startsWith("ru")) return "ru";
  if (nav.startsWith("zh")) return "zh";
  return "en";
}

const lang = ref(initialLang());

export function useI18n() {
  const t = computed(() => {
    const table = messages[lang.value] || messages.en;
    return (key) => table[key] ?? messages.en[key] ?? key;
  });
  const setLang = (id) => {
    if (!messages[id]) return;
    lang.value = id;
    try {
      localStorage.setItem(KEY, id);
    } catch { /* ignore */ }
    document.documentElement.lang = id === "zh" ? "zh-CN" : id;
  };
  return { lang, setLang, t };
}
