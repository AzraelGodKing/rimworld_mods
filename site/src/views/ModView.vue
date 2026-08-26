<script setup>
import { computed } from "vue";
import { useRoute } from "vue-router";
import modsData from "../data/mods.json";
import FeatureTabs from "../components/FeatureTabs.vue";
import GalleryLightbox from "../components/GalleryLightbox.vue";
import CollapsibleList from "../components/CollapsibleList.vue";
import { useStats } from "../composables/useStats.js";
import { compatTarget } from "../lib/compatLinks.js";
import { useI18n } from "../composables/useI18n.js";

const route = useRoute();
const { forMod, format } = useStats();
const { t } = useI18n();
const BASE = import.meta.env.BASE_URL;

const mod = computed(() => modsData.mods.find((m) => m.id === route.params.id));
const stats = computed(() => forMod(route.params.id).value);

function targetFor(entry) {
  return compatTarget(entry, modsData.mods);
}
</script>

<template>
  <article v-if="mod" class="mod-page" :style="{ '--mod': mod.accent }">
    <section class="hero mod-hero">
      <div class="wrap">
        <RouterLink class="back-link" to="/">← {{ t('mod.backToMods') }}</RouterLink>
        <div class="mod-hero-grid">
          <div>
            <h1>{{ mod.name }}</h1>
            <p class="hero-tagline">{{ mod.tagline }}</p>
            <div class="mod-badges">
              <span v-for="b in mod.badges" :key="b" class="badge">{{ b }}</span>
            </div>
            <p class="mod-hero-stats" v-if="stats" aria-live="polite">
              {{ t('mod.workshop') }}:
              <strong>{{ format(stats.subscriptions) }}</strong> {{ t('stats.subscribers') }}
              · <strong>{{ format(stats.favorited) }}</strong> {{ t('stats.favorites') }}
              · <strong>{{ format(stats.views) }}</strong> {{ t('stats.views') }}
            </p>
            <p class="mod-cta">
              <a class="btn btn-mod" :href="mod.workshopUrl" target="_blank" rel="noopener">
                {{ t('mod.workshop') }} →
              </a>
              <RouterLink class="btn btn-outline" :to="`/${mod.id}/changelog`">
                {{ t('mod.changelog') }}
              </RouterLink>
            </p>
          </div>
          <div class="mod-hero-media">
            <img :src="BASE + mod.preview" :alt="`${mod.name} preview`">
          </div>
        </div>
      </div>
    </section>

    <section class="wrap section">
      <h2>{{ t('mod.overview') }}</h2>
      <p v-for="p in mod.overview" :key="p" class="overview-para">{{ p }}</p>
    </section>

    <section class="wrap section" v-if="mod.featureTabs?.length">
      <h2>{{ t('mod.features') }}</h2>
      <FeatureTabs :tabs="mod.featureTabs" />
    </section>

    <section class="wrap section" v-if="mod.gallery?.length">
      <h2>{{ t('mod.gallery') }}</h2>
      <GalleryLightbox :images="mod.gallery" />
    </section>

    <section class="wrap section" v-if="mod.goodToKnow?.length">
      <CollapsibleList :title="t('mod.goodToKnow')" :items="mod.goodToKnow" :open="false" />
    </section>

    <section class="wrap section" v-if="mod.compatibility">
      <h2>{{ t('mod.compat') }}</h2>
      <p class="compat-lists-link">
        {{ t('compat.seeLists') }}
        <RouterLink to="/compat/compatible">{{ t('compat.tab.compatible') }}</RouterLink>
        ·
        <RouterLink to="/compat/incompatible">{{ t('compat.tab.incompatible') }}</RouterLink>
      </p>
      <div class="compat-cols">
        <div v-if="mod.compatibility.compatibleWith?.length">
          <h3>{{ t('mod.compatibleWith') }}</h3>
          <ul class="compat-list ok">
            <li v-for="c in mod.compatibility.compatibleWith" :key="c.name">
              <a
                v-if="targetFor(c)?.external"
                :href="targetFor(c).href"
                target="_blank"
                rel="noopener"
              >{{ c.name }}</a>
              <RouterLink v-else-if="targetFor(c)" :to="targetFor(c).href">{{ c.name }}</RouterLink>
              <strong v-else>{{ c.name }}</strong>
              <span v-if="c.note"> — {{ c.note }}</span>
            </li>
          </ul>
        </div>
        <div v-if="mod.compatibility.incompatibleWith?.length">
          <h3>{{ t('mod.incompatibleWith') }}</h3>
          <ul class="compat-list bad">
            <li v-for="c in mod.compatibility.incompatibleWith" :key="c.name">
              <a
                v-if="targetFor(c)?.external"
                :href="targetFor(c).href"
                target="_blank"
                rel="noopener"
              >{{ c.name }}</a>
              <RouterLink v-else-if="targetFor(c)" :to="targetFor(c).href">{{ c.name }}</RouterLink>
              <strong v-else>{{ c.name }}</strong>
              <span v-if="c.note"> — {{ c.note }}</span>
            </li>
          </ul>
        </div>
      </div>
      <ul v-if="mod.compatibility.notes?.length" class="compat-notes">
        <li v-for="n in mod.compatibility.notes" :key="n">{{ n }}</li>
      </ul>
    </section>
  </article>
</template>
