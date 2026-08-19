import { createRouter, createWebHistory } from "vue-router";
import modsData from "./data/mods.json";

const HomeView = () => import("./views/HomeView.vue");
const ModView = () => import("./views/ModView.vue");
const CompatView = () => import("./views/CompatView.vue");
const NotFoundView = () => import("./views/NotFoundView.vue");

const modIds = new Set(modsData.mods.map((m) => m.id));

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name: "home", component: HomeView },
    { path: "/compat", name: "compat", component: CompatView },
    {
      path: "/:id",
      name: "mod",
      component: ModView,
      beforeEnter(to) {
        if (!modIds.has(to.params.id)) return { name: "not-found", params: { pathMatch: to.path.slice(1).split("/") } };
      },
    },
    { path: "/:pathMatch(.*)*", name: "not-found", component: NotFoundView },
  ],
  scrollBehavior(to, from, saved) {
    if (saved) return saved;
    if (to.hash) return { el: to.hash, behavior: "smooth" };
    return { top: 0 };
  },
});

router.afterEach((to) => {
  const mod = modsData.mods.find((m) => m.id === to.params.id);
  document.title = mod
    ? `${mod.name} — a RimWorld mod by ${modsData.site.author}`
    : to.name === "compat"
      ? `Compatibility — ${modsData.site.title}`
      : `${modsData.site.title} — ${modsData.site.author}`;
});
