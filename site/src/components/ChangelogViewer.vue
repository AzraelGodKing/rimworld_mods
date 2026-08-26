<script setup>
import { ref, computed, watchEffect } from "vue";
import { marked } from "marked";
import { useI18n } from "../composables/useI18n.js";

const props = defineProps({
  changelogPath: { type: String, required: true }, // e.g. "changelogs/strata.md"
});

const { t } = useI18n();

const changelogs = import.meta.glob("../data/changelogs/*.md", {
  query: "?raw",
  import: "default",
});

const raw = ref(null);
const filter = ref("");

watchEffect(async () => {
  raw.value = null;
  const file = props.changelogPath.replace(/^.*\//, "");
  const loader =
    changelogs[`../data/changelogs/${file}`] ||
    Object.entries(changelogs).find(([k]) => k.endsWith(`/${file}`))?.[1];
  raw.value = loader ? await loader() : "";
});

function parseSections(md) {
  if (!md) return [];
  const chunks = md.split(/^## /m);
  return chunks.slice(1).map((chunk) => {
    const nl = chunk.indexOf("\n");
    const title = (nl === -1 ? chunk : chunk.slice(0, nl)).trim();
    const body = nl === -1 ? "" : chunk.slice(nl + 1).trim();
    return { title, body };
  }).filter((s) => s.body);
}

function sectionHtml(section) {
  return marked.parse(`## ${section.title}\n\n${section.body}`, {
    gfm: true,
    breaks: false,
  });
}

const sections = computed(() => {
  const all = parseSections(raw.value || "");
  const q = filter.value.trim().toLowerCase();
  if (!q) return all;
  return all.filter(
    (s) =>
      s.title.toLowerCase().includes(q) || s.body.toLowerCase().includes(q)
  );
});

const latest = computed(() => sections.value[0] || null);
const older = computed(() => sections.value.slice(1));
</script>

<template>
  <div class="changelog">
    <input
      v-model="filter"
      type="search"
      class="changelog-filter"
      :placeholder="t('changelog.search')"
    >
    <div v-if="raw === null" class="changelog-body">…</div>
    <p v-else-if="!latest" class="changelog-empty">{{ t('changelog.empty') }}</p>
    <template v-else>
      <article class="changelog-latest">
        <p class="changelog-kicker">{{ t('changelog.latest') }}</p>
        <div class="changelog-body" v-html="sectionHtml(latest)"></div>
      </article>
      <details
        v-if="older.length"
        class="changelog-history"
        :open="!!filter.trim()"
      >
        <summary>{{ t('changelog.history') }} ({{ older.length }})</summary>
        <details
          v-for="(section, i) in older"
          :key="i"
          class="changelog-version"
        >
          <summary>{{ section.title }}</summary>
          <div class="changelog-body" v-html="sectionHtml(section)"></div>
        </details>
      </details>
    </template>
  </div>
</template>
