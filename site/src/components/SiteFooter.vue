<script setup>
import modsData from "../data/mods.json";
import { useI18n } from "../composables/useI18n.js";

const { t } = useI18n();
const year = new Date().getFullYear();
</script>

<template>
  <footer class="site-footer">
    <div class="footer-inner">
      <p>© {{ year }} {{ modsData.site.author }} — RimWorld mods.</p>
      <p>
        <a :href="modsData.site.github" target="_blank" rel="noopener">{{ t('footer.source') }}</a>
      </p>
    </div>
  </footer>
</template>
