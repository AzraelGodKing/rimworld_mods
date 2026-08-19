<script setup>
import { ref, watch } from "vue";

const props = defineProps({
  tabs: { type: Array, required: true },
});

const active = ref(props.tabs[0]?.id);
watch(
  () => props.tabs,
  (tabs) => {
    if (!tabs.some((t) => t.id === active.value)) active.value = tabs[0]?.id;
  }
);
</script>

<template>
  <div class="feature-tabs">
    <div class="tab-list" role="tablist">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        role="tab"
        :aria-selected="active === tab.id"
        :class="{ active: active === tab.id }"
        @click="active = tab.id"
      >{{ tab.label }}</button>
    </div>
    <template v-for="tab in tabs" :key="tab.id">
      <Transition name="tab">
        <div v-if="active === tab.id" class="tab-panel" role="tabpanel">
          <div class="feature-grid">
            <div v-for="f in tab.features" :key="f.title" class="feature-card">
              <h4>
                {{ f.title }}
                <span v-if="f.tag" class="feature-tag">{{ f.tag }}</span>
              </h4>
              <p>{{ f.body }}</p>
            </div>
          </div>
        </div>
      </Transition>
    </template>
  </div>
</template>
