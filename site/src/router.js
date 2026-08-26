import { createRouter, createWebHistory } from "vue-router";
import modsData from "./data/mods.json";

const HomeView = () => import("./views/HomeView.vue");
const ModView = () => import("./views/ModView.vue");
const ChangelogView = () => import("./views/ChangelogView.vue");
const CompatView = () => import("./views/CompatView.vue");
const NotFoundView = () => import("./views/NotFoundView.vue");

const modIds = new Set(modsData.mods.map((m) => m.id));

function requireMod(to) {
  if (!modIds.has(to.params.id)) {
    return { name: "not-found", params: { pathMatch: to.path.slice(1).split("/") } };
  }
}

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name: "home", component: HomeView },
    { path: "/compat", name: "compat", component: CompatView },
    { path: "/compat/compatible", name: "compat-ok", component: CompatView },
    { path: "/compat/incompatible", name: "compat-bad", component: CompatView },
    {
      path: "/:id/changelog",
      name: "mod-changelog",
      component: ChangelogView,
      beforeEnter: requireMod,
    },
    {
      path: "/:id",
      name: "mod",
      component: ModView,
      beforeEnter: requireMod,
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
  if (mod && to.name === "mod-changelog") {
    document.title = `${mod.name} changelog — a RimWorld mod by ${modsData.site.author}`;
    return;
  }
  if (mod) {
    document.title = `${mod.name} — a RimWorld mod by ${modsData.site.author}`;
    return;
  }
  if (to.name === "compat-ok") {
    document.title = `Compatible mods — ${modsData.site.title}`;
    return;
  }
  if (to.name === "compat-bad") {
    document.title = `Incompatible mods — ${modsData.site.title}`;
    return;
  }
  document.title =
    to.name === "compat"
      ? `Compatibility — ${modsData.site.title}`
      : `${modsData.site.title} — ${modsData.site.author}`;
});
