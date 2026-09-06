<script setup>
import { ref } from "vue";
import { useStats } from "../composables/useStats.js";
import { useI18n } from "../composables/useI18n.js";

const { state, format, updatedLabel, refreshStats } = useStats();
const { t } = useI18n();
const failed = ref(false);

async function onRefresh() {
  failed.value = false;
  try {
    localStorage.removeItem("azrael-workshop-stats-v3");
  } catch { /* ignore */ }
  failed.value = !(await refreshStats({ force: true }));
}
</script>

<template>
  <div class="stats-bar" aria-live="polite">
    <p class="stats-primary" v-if="state.siteTotal?.subscriptions || state.siteTotal?.favorited">
      <strong>{{ t('hub.allMods') }}:</strong>
      <span class="stat-num">{{ format(state.siteTotal?.subscriptions) }}</span> {{ t('stats.subscribers') }}
      ·
      <span class="stat-num">{{ format(state.siteTotal?.favorited) }}</span> {{ t('stats.favorites') }}
    </p>
    <p class="stats-primary">
      <strong>{{ t('hub.allNexus') }}:</strong>
      <span class="stat-num">{{ format(state.siteTotal?.nexus_downloads) }}</span> {{ t('stats.downloads') }}
      ·
      <span class="stat-num">{{ format(state.siteTotal?.nexus_endorsements) }}</span> {{ t('stats.endorsements') }}
    </p>
    <p class="stats-detail">
      {{ t('stats.updated') }} {{ updatedLabel }} · {{ t('stats.live') }} ·
      <button class="stats-refresh" :disabled="state.loading" @click="onRefresh">
        {{ state.loading ? t('stats.refreshing') : failed ? t('stats.failed') : t('stats.refresh') }}
      </button>
    </p>
  </div>
</template>
