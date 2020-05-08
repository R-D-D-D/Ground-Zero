const router = new VueRouter({
    routes: [{
      path: '/',
      name: 'home',
      component: comps['v-list'],
    }, {
      path: '/login',
      name: 'login',
      component: comps['v-login'],
    }, {
      path: '/student',
      name: 'student',
      component: comps['v-student']
    }]
});

// router.beforeEach((to, from, next) => {
//   if (to.name !== 'login' && !store.getters.logged_in) next({ name: 'login' });
//   else next();
// })

var app = new Vue({ 
    el: '#app',
    store,
    router,
    computed: {
        tkn() {
            return this.$store.getters.token;
        },
        albums() {
            return this.$store.getters.albums;
        },
        email() {
            return this.$store.getters.email;
        },
        logged_in() {
            return this.$store.getters.logged_in;
        }
    }
}).$mount('#app');
