<script setup>
import { useStats } from "../composables/useStats.js";
import { useI18n } from "../composables/useI18n.js";

const props = defineProps({
  mod: { type: Object, required: true },
});

const { forMod, format } = useStats();
const { t } = useI18n();
const stats = forMod(props.mod.id);
const BASE = import.meta.env.BASE_URL;
</script>

<template>
  <RouterLink class="mod-card" :to="`/${mod.id}`" :style="{ '--mod': mod.accent }">
    <div class="mod-card-media">
      <img :src="BASE + mod.preview" :alt="`${mod.name} preview`" loading="lazy">
    </div>
    <div class="mod-card-body">
      <h2>{{ mod.name }}</h2>
      <p class="mod-card-tagline">{{ mod.tagline }}</p>
      <div class="mod-card-stats" v-if="stats && (stats.subscriptions || stats.favorited)">
        <span>{{ format(stats.subscriptions) }} {{ t('stats.subs') }}</span>
        <span>{{ format(stats.favorited) }} {{ t('stats.favs') }}</span>
      </div>
      <div class="mod-card-stats mod-card-stats-nexus" v-if="stats && mod.nexusModId">
        <span>{{ format(stats.nexus_downloads) }} {{ t('stats.dls') }}</span>
        <span>{{ format(stats.nexus_endorsements) }} {{ t('stats.endo') }}</span>
      </div>
      <div class="mod-card-badges">
        <span v-for="b in mod.badges.slice(0, 3)" :key="b" class="badge">{{ b }}</span>
      </div>
    </div>
  </RouterLink>
</template>
