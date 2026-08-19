<script setup>
import { useI18n } from "../composables/useI18n.js";

const { t } = useI18n();
</script>

<template>
  <div class="wrap section not-found">
    <h1>{{ t('notFound.title') }}</h1>
    <p>{{ t('notFound.body') }}</p>
    <p><RouterLink class="btn btn-mod" to="/">{{ t('notFound.home') }}</RouterLink></p>
  </div>
</template>
