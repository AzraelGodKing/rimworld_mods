<script setup>
import { ref, computed } from "vue";
import modsData from "../data/mods.json";
import ModCard from "../components/ModCard.vue";
import StatsBar from "../components/StatsBar.vue";
import { useStats } from "../composables/useStats.js";
import { useI18n } from "../composables/useI18n.js";

const { state } = useStats();
const { t } = useI18n();

const query = ref("");
const sortBy = ref("default");

const sortOptions = [
  { id: "default", key: "hub.sort.default" },
  { id: "subs", key: "hub.sort.subs" },
  { id: "favs", key: "hub.sort.favs" },
  { id: "name", key: "hub.sort.name" },
];

function haystack(mod) {
  return [
    mod.name,
    mod.tagline,
    ...(mod.badges || []),
    ...(mod.overview || []),
    ...(mod.featureTabs || []).flatMap((tab) => [
      tab.label,
      ...tab.features.flatMap((f) => [f.title, f.body]),
    ]),
  ]
    .join(" ")
    .toLowerCase();
}

const filtered = computed(() => {
  const q = query.value.trim().toLowerCase();
  let mods = modsData.mods;
  if (q) mods = mods.filter((m) => haystack(m).includes(q));
  if (sortBy.value === "name") {
    mods = [...mods].sort((a, b) => a.name.localeCompare(b.name));
  } else if (sortBy.value === "subs" || sortBy.value === "favs") {
    const field = sortBy.value === "subs" ? "subscriptions" : "favorited";
    mods = [...mods].sort(
      (a, b) => (state.mods[b.id]?.[field] ?? 0) - (state.mods[a.id]?.[field] ?? 0)
    );
  }
  return mods;
});
</script>

<template>
  <div class="home">
    <section class="hero">
      <div class="wrap">
        <h1>{{ modsData.site.title }}</h1>
        <p class="hero-tagline">{{ modsData.site.tagline }}</p>
        <p class="hero-intro">{{ modsData.site.heroIntro }}</p>
        <StatsBar />
      </div>
    </section>

    <section class="wrap">
      <div class="hub-controls">
        <input
          v-model="query"
          type="search"
          class="hub-search"
          :placeholder="t('hub.search')"
          :aria-label="t('hub.search')"
        >
        <select v-model="sortBy" class="hub-sort" aria-label="Sort">
          <option v-for="o in sortOptions" :key="o.id" :value="o.id">{{ t(o.key) }}</option>
        </select>
      </div>

      <TransitionGroup name="grid" tag="div" class="hub-grid">
        <ModCard v-for="mod in filtered" :key="mod.id" :mod="mod" />
      </TransitionGroup>
      <p v-if="!filtered.length" class="hub-empty">—</p>
    </section>
  </div>
</template>
