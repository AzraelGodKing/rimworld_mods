<script setup>
import { computed } from "vue";
import { useRoute } from "vue-router";
import modsData from "../data/mods.json";
import ChangelogViewer from "../components/ChangelogViewer.vue";
import { useI18n } from "../composables/useI18n.js";

const route = useRoute();
const { t } = useI18n();

const mod = computed(() => modsData.mods.find((m) => m.id === route.params.id));
const backLabel = computed(() =>
  t.value("changelog.backToMod").replace("{name}", mod.value?.name ?? "")
);
</script>

<template>
  <article v-if="mod" class="mod-page changelog-page" :style="{ '--mod': mod.accent }">
    <section class="hero mod-hero">
      <div class="wrap">
        <RouterLink class="back-link" :to="`/${mod.id}`">{{ backLabel }}</RouterLink>
        <h1>{{ mod.name }}</h1>
        <p class="hero-tagline">{{ t('mod.changelog') }}</p>
      </div>
    </section>
    <section class="wrap section">
      <ChangelogViewer :changelog-path="mod.changelogPath" />
    </section>
  </article>
</template>
