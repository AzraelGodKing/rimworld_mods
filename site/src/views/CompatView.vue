<script setup>
import { ref, computed } from "vue";
import modsData from "../data/mods.json";
import { useI18n } from "../composables/useI18n.js";

const { t } = useI18n();
const selected = ref(new Set());

function toggle(id) {
  const next = new Set(selected.value);
  if (next.has(id)) next.delete(id);
  else next.add(id);
  selected.value = next;
}

// Notes relevant to the selection: a selected mod's own notes, plus its
// compat entries that mention another selected mod (or any external mod).
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
</script>

<template>
  <div class="wrap section compat-page">
    <h1>{{ t('compat.title') }}</h1>
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
  </div>
</template>
