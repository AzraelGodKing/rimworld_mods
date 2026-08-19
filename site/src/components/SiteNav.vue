<script setup>
import { ref } from "vue";
import modsData from "../data/mods.json";
import { useTheme } from "../composables/useTheme.js";
import { useI18n, LANGS } from "../composables/useI18n.js";

const { mode, toggle } = useTheme();
const { lang, setLang, t } = useI18n();
const open = ref(false);
</script>

<template>
  <header class="site-nav">
    <div class="nav-inner">
      <RouterLink class="brand" to="/" @click="open = false">
        <span class="brand-mark">AZ</span>
        <span class="brand-name">{{ modsData.site.author }}</span>
      </RouterLink>

      <button
        class="nav-burger"
        :aria-expanded="open"
        aria-label="Menu"
        @click="open = !open"
      >☰</button>

      <nav class="nav-links" :class="{ open }" @click="open = false">
        <RouterLink
          v-for="mod in modsData.mods"
          :key="mod.id"
          :to="`/${mod.id}`"
          class="nav-mod"
          :style="{ '--mod': mod.accent }"
        >{{ mod.name }}</RouterLink>
        <RouterLink to="/compat" class="nav-compat">{{ t('nav.compat') }}</RouterLink>
      </nav>

      <div class="nav-tools">
        <div class="lang-switch" role="group" aria-label="Language">
          <button
            v-for="l in LANGS"
            :key="l.id"
            :class="{ active: lang === l.id }"
            @click="setLang(l.id)"
          >{{ l.label }}</button>
        </div>
        <button class="theme-toggle" :title="t('nav.theme')" :aria-label="t('nav.theme')" @click="toggle">
          {{ mode === 'dark' ? '☀' : '☾' }}
        </button>
      </div>
    </div>
  </header>
</template>
