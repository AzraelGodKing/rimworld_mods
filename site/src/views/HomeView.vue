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
const BASE = import.meta.env.BASE_URL;

const sortOptions = [
  { id: "default", key: "hub.sort.default" },
  { id: "subs", key: "hub.sort.subs" },
  { id: "favs", key: "hub.sort.favs" },
  { id: "name", key: "hub.sort.name" },
];

const heroArt = computed(() =>
  modsData.mods.slice(0, 4).map((m) => ({
    id: m.id,
    src: BASE + m.preview,
    name: m.name,
  }))
);

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
    <section class="hero" aria-labelledby="hero-brand">
      <div class="hero-plane" aria-hidden="true">
        <div class="hero-wash"></div>
        <div class="hero-grain"></div>
        <div class="hero-embers"></div>
        <div class="hero-collage">
          <figure
            v-for="(shot, i) in heroArt"
            :key="shot.id"
            class="hero-shot"
            :class="`hero-shot-${i + 1}`"
          >
            <img :src="shot.src" :alt="''" loading="eager">
          </figure>
        </div>
      </div>

      <div class="hero-stage wrap">
        <div class="hero-copy">
          <p id="hero-brand" class="hero-brand">{{ modsData.site.author }}</p>
          <h1 class="hero-headline">{{ t('hub.headline') }}</h1>
          <p class="hero-lede">{{ modsData.site.tagline }}</p>
          <div class="hero-cta">
            <a class="btn btn-ember" href="#workshop">{{ t('hub.cta.browse') }}</a>
            <a
              class="btn btn-ghost"
              :href="modsData.site.github"
              target="_blank"
              rel="noopener"
            >{{ t('hub.cta.source') }}</a>
            <RouterLink class="btn btn-ghost" to="/compat">{{ t('nav.compat') }}</RouterLink>
          </div>
        </div>
      </div>
    </section>

    <section id="workshop" class="workshop wrap">
      <header class="workshop-head">
        <h2 class="workshop-title">{{ t('hub.workshop') }}</h2>
        <p class="workshop-note">{{ modsData.site.heroIntro }}</p>
      </header>

      <StatsBar />

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
