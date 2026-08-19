<script setup>
import { ref, computed, watchEffect } from "vue";
import { marked } from "marked";
import { useI18n } from "../composables/useI18n.js";

const props = defineProps({
  changelogPath: { type: String, required: true }, // repo-root-relative, e.g. "Strata/CHANGELOG.md"
});

const { t } = useI18n();

// Bundle every top-level mod CHANGELOG.md at build time.
const changelogs = import.meta.glob("../../../*/CHANGELOG.md", {
  query: "?raw",
  import: "default",
});

const raw = ref(null);
const filter = ref("");

watchEffect(async () => {
  raw.value = null;
  const key = `../../../${props.changelogPath}`;
  const loader = changelogs[key];
  raw.value = loader ? await loader() : "";
});

const html = computed(() => {
  if (raw.value == null) return null;
  let text = raw.value;
  if (filter.value.trim()) {
    const q = filter.value.trim().toLowerCase();
    // keep headings for context plus matching bullet lines
    text = text
      .split("\n")
      .filter((line) => line.startsWith("#") || line.toLowerCase().includes(q))
      .join("\n");
  }
  return marked.parse(text, { gfm: true, breaks: false });
});
</script>

<template>
  <div class="changelog">
    <input
      v-model="filter"
      type="search"
      class="changelog-filter"
      :placeholder="t('hub.search')"
    >
    <div v-if="html === null" class="changelog-body">…</div>
    <div v-else-if="!raw" class="changelog-body">{{ t('changelog.empty') }}</div>
    <div v-else class="changelog-body" v-html="html"></div>
  </div>
</template>
