<script setup>
import { ref, onMounted, onUnmounted } from "vue";
import { useI18n } from "../composables/useI18n.js";

const props = defineProps({
  images: { type: Array, required: true }, // [{ src, caption }]
});

const { t } = useI18n();
const BASE = import.meta.env.BASE_URL;
const openIndex = ref(-1);

function open(i) {
  openIndex.value = i;
}
function close() {
  openIndex.value = -1;
}
function step(delta) {
  const n = props.images.length;
  openIndex.value = (openIndex.value + delta + n) % n;
}
function onKey(e) {
  if (openIndex.value < 0) return;
  if (e.key === "Escape") close();
  else if (e.key === "ArrowLeft") step(-1);
  else if (e.key === "ArrowRight") step(1);
}

onMounted(() => window.addEventListener("keydown", onKey));
onUnmounted(() => window.removeEventListener("keydown", onKey));
</script>

<template>
  <div class="gallery">
    <button
      v-for="(img, i) in images"
      :key="img.src"
      class="gallery-thumb"
      @click="open(i)"
    >
      <img :src="BASE + img.src" :alt="img.caption" loading="lazy">
      <span class="gallery-caption">{{ img.caption }}</span>
    </button>

    <Teleport to="body">
      <Transition name="lightbox">
        <div v-if="openIndex >= 0" class="lightbox" role="dialog" aria-modal="true" @click.self="close">
          <button class="lightbox-close" :aria-label="t('lightbox.close')" @click="close">×</button>
          <button v-if="images.length > 1" class="lightbox-nav prev" :aria-label="t('lightbox.prev')" @click="step(-1)">‹</button>
          <figure>
            <img :src="BASE + images[openIndex].src" :alt="images[openIndex].caption">
            <figcaption>{{ images[openIndex].caption }} ({{ openIndex + 1 }}/{{ images.length }})</figcaption>
          </figure>
          <button v-if="images.length > 1" class="lightbox-nav next" :aria-label="t('lightbox.next')" @click="step(1)">›</button>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
