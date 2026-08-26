<script setup>
import { ref, computed } from "vue";
import { useRoute } from "vue-router";
import modsData from "../data/mods.json";
import { compatTarget } from "../lib/compatLinks.js";
import { useI18n } from "../composables/useI18n.js";

const { t } = useI18n();
const route = useRoute();
const selected = ref(new Set());
const listQuery = ref("");

const tab = computed(() => {
  if (route.name === "compat-ok") return "ok";
  if (route.name === "compat-bad") return "bad";
  return "checker";
});

function toggle(id) {
  const next = new Set(selected.value);
  if (next.has(id)) next.delete(id);
  else next.add(id);
  selected.value = next;
}

const notes = computed(() => {
  const picked = modsData.mods.filter((m) => selected.value.has(m.id));
  const pickedNames = picked.map((m) => m.name.toLowerCase());
  const out = [];
  for (const mod of picked) {
    const compat = mod.compatibility || {};
    for (const c of compat.compatibleWith || []) {
      const other = c.name.toLowerCase();
      if (pickedNames.some((n) => n !== mod.name.toLowerCase() && other.includes(n))) {
        out.push({ mod: mod.name, kind: "ok", text: `${c.name}${c.note ? " — " + c.note : ""}` });
      }
    }
    for (const c of compat.incompatibleWith || []) {
      out.push({ mod: mod.name, kind: "bad", text: `${c.name}${c.note ? " — " + c.note : ""}` });
    }
    for (const n of compat.notes || []) {
      out.push({ mod: mod.name, kind: "note", text: n });
    }
  }
  return out;
});

function targetFor(entry) {
  return compatTarget(entry, modsData.mods);
}

function catalog(kind) {
  const map = new Map();
  for (const mod of modsData.mods) {
    for (const c of mod.compatibility?.[kind] || []) {
      const key = c.name.toLowerCase();
      if (!map.has(key)) {
        map.set(key, {
          name: c.name,
          url: c.url || "",
          id: c.id || "",
          entries: [],
        });
      }
      const row = map.get(key);
      if (c.url && !row.url) row.url = c.url;
      if (c.id && !row.id) row.id = c.id;
      row.entries.push({
        mod: mod.name,
        modId: mod.id,
        note: c.note || "",
        accent: mod.accent,
      });
    }
  }
  return [...map.values()]
    .map((row) => ({ ...row, target: targetFor(row) }))
    .sort((a, b) => a.name.localeCompare(b.name));
}

const compatibleCatalog = computed(() => catalog("compatibleWith"));
const incompatibleCatalog = computed(() => catalog("incompatibleWith"));

const visibleCatalog = computed(() => {
  const all = tab.value === "ok" ? compatibleCatalog.value : incompatibleCatalog.value;
  const q = listQuery.value.trim().toLowerCase();
  if (!q) return all;
  return all.filter(
    (row) =>
      row.name.toLowerCase().includes(q) ||
      row.entries.some(
        (e) => e.mod.toLowerCase().includes(q) || e.note.toLowerCase().includes(q)
      )
  );
});
</script>

<template>
  <div class="wrap section compat-page">
    <h1>{{ t('compat.title') }}</h1>
    <p class="hero-intro">{{ t('compat.pageIntro') }}</p>

    <div class="tab-list" role="tablist">
      <RouterLink
        to="/compat"
        class="compat-tab"
        :class="{ active: tab === 'checker' }"
        role="tab"
        :aria-selected="tab === 'checker'"
      >{{ t('compat.tab.checker') }}</RouterLink>
      <RouterLink
        to="/compat/compatible"
        class="compat-tab"
        :class="{ active: tab === 'ok' }"
        role="tab"
        :aria-selected="tab === 'ok'"
      >{{ t('compat.tab.compatible') }}</RouterLink>
      <RouterLink
        to="/compat/incompatible"
        class="compat-tab"
        :class="{ active: tab === 'bad' }"
        role="tab"
        :aria-selected="tab === 'bad'"
      >{{ t('compat.tab.incompatible') }}</RouterLink>
    </div>

    <template v-if="tab === 'checker'">
      <p class="hero-intro">{{ t('compat.intro') }}</p>
      <h2>{{ t('compat.pick') }}</h2>
      <div class="compat-picker">
        <button
          v-for="mod in modsData.mods"
          :key="mod.id"
          class="compat-chip"
          :class="{ selected: selected.has(mod.id) }"
          :style="{ '--mod': mod.accent }"
          :aria-pressed="selected.has(mod.id)"
          @click="toggle(mod.id)"
        >{{ mod.name }}</button>
      </div>

      <template v-if="selected.size">
        <h2>{{ t('compat.result') }}</h2>
        <p v-if="!notes.length" class="compat-ok-msg">{{ t('compat.none') }}</p>
        <ul v-else class="compat-results">
          <li v-for="(n, i) in notes" :key="i" :class="n.kind">
            <strong>{{ n.mod }}</strong>: {{ n.text }}
          </li>
        </ul>
      </template>
    </template>

    <template v-else>
      <p class="hero-intro">{{ tab === 'ok' ? t('compat.listIntroOk') : t('compat.listIntroBad') }}</p>
      <input
        v-model="listQuery"
        type="search"
        class="changelog-filter"
        :placeholder="t('compat.listSearch')"
        :aria-label="t('compat.listSearch')"
      >
      <p v-if="!visibleCatalog.length" class="compat-ok-msg">{{ t('compat.listEmpty') }}</p>
      <ul v-else class="compat-catalog" :class="tab">
        <li v-for="row in visibleCatalog" :key="row.name">
          <div class="compat-catalog-head">
            <a
              v-if="row.target?.external"
              class="compat-catalog-name"
              :href="row.target.href"
              target="_blank"
              rel="noopener"
            >{{ row.name }} →</a>
            <RouterLink
              v-else-if="row.target"
              class="compat-catalog-name"
              :to="row.target.href"
            >{{ row.name }}</RouterLink>
            <strong v-else class="compat-catalog-name">{{ row.name }}</strong>
          </div>
          <ul class="compat-catalog-notes">
            <li v-for="e in row.entries" :key="e.modId">
              <RouterLink :to="`/${e.modId}`" :style="{ color: e.accent }">{{ e.mod }}</RouterLink>
              <span v-if="e.note"> — {{ e.note }}</span>
            </li>
          </ul>
        </li>
      </ul>
    </template>
  </div>
</template>
