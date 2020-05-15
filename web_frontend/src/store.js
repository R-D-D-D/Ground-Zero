const store = new Vuex.Store({
    state: {
        email: 'wangrunding@gmail.com',
        tkn: '03c0922d0c2479e30281056ad64f85394f11ac6d',
        albums: [],
        logged_in: false
    },
    getters: {
        token: state => state.tkn,
        albums: state => state.albums,
        email: state => state.email,
        logged_in: state => state.logged_in,
        album: state => id => {
            return state.albums.filter(album => album.info.id == id)[0];
        }
    },
    mutations: {
        enterEmail(state, payload) {
            state.email = payload.email;
            state.tkn = payload.tkn;
        },

        updateAlbums(state, albums) {
            state.albums = albums;
        },

        login(state) {
            state.logged_in = true;
        },

        //update the categories of an album with id payload.id
        updateCategories(state, payload) {
            // let index = state.albums.findIndex(x => x.info.id.localeCompare(payload.id) == 0)
            // state.albums[index].categories = payload.categories
            payload.album.categories = payload.categories;
        },

        // update the whole array of photos of a category
        updatePhotos(state, payload) {
            payload.category.photos = payload.photos;
        },

        //update single photo of a category
        updatePhoto(state, payload) {
            payload.category.photos.push(payload.photo);
        },

        updateCoverPhoto(state, payload) {
            payload.album.cover_photo[0] = payload.photo;
        }
    },

    actions: {
        enterEmail(store, email) {
            api.enterEmail(email, data => {
                //console.log(data)
                store.commit('enterEmail', {
                    email: email, 
                    tkn: data.data.tkn})
                alert('Now retrieve the pass from your email')
            });
        },

        login(store, pass) {
            api.logIn(store.getters.token, pass, data => {
                //console.log(data)
                store.commit('login');
                store.dispatch('updateAlbums', {
                    login: true
                });
                alert('Log in success!');
            })
        },

        adminLogin(store) {
            store.commit('login');
            store.dispatch('updateAlbums', {
                login: true
            });
        },

        // this is to keep the list of albums updated, internally it calls
        // updateCategories
        updateAlbums(store, payload) {
            api.getAlbums(data => {
                //console.log(data)
                var temp = [];
                data.data.albums.forEach(element => {
                    temp.push({
                        info: element, 
                        categories: [],
                        cover_photo: []
                    })
                });
                store.commit('updateAlbums', temp);
                store.dispatch('updateCategories', payload);
            }, store.getters.token);
        },

        // this is to update the categories, internally it calls updatePhotos and updateCoverPhoto
        updateCategories(store, payload) {
            var albums = store.getters.albums;
            albums.forEach(element => {
                api.getCategories(element.info.id, data => {
                    let obj = {
                        aid: element.info.id
                    }
                    if (element == albums[albums.length - 1] && payload) {
                        Object.assign(obj, payload);
                    }
                    store.dispatch('updateCoverPhoto', obj);
                    store.commit('updateCategories', {
                        album: element,
                        categories: Object.assign(data.data.categories, { photos: [] })
                    });
                    element.categories.forEach(category => {
                        store.dispatch('updatePhotos', {
                            category: category,
                            album: element
                        })
                    })
                }, store.getters.token);
            });
        },

        updatePhotos(store, payload) {
            api.getPhotos(payload.category.id, data => {
                store.commit('updatePhotos', {
                    category: payload.category,
                    photos: data.data.photos
                })
            }, store.getters.token);
        },

        updateCoverPhoto(store, payload) {
            api.getCoverPhoto(payload.aid, data => {
                store.commit('updateCoverPhoto', {
                    album: store.getters.album(payload.aid),
                    photo: data.data.photos[0]
                })
                if (payload.login) {
                    router.push('/');
                }
            }, store.getters.token);
        },

        createAlbum(store, payload) {
            api.createAlbum(payload.name, data => {
                //console.log(data)
                payload.categories.forEach(x => {
                    api.addCategory(x, data.data.id, () => {}, store.getters.token)
                });
                store.dispatch('addCoverPhoto', {
                    id: data.data.id,
                    photo: payload.photo
                });
            }, store.getters.token);
        },

        deleteAlbum(store, id) {
            api.deleteAlbum(id, data => {
                alert('success');
                //console.log(data)
                store.dispatch('updateAlbums');
                router.push('/');
            }, store.getters.token);
        },

        //this is to update a single album
        updateAlbum(store, payload) {
            api.updateAlbum(payload.album.info.name, payload.album.info.id, data => {
                alert('success');
                payload.categories.forEach(cat => {
                    if (payload.album.categories.findIndex(x => x.category.localeCompare(cat) == 0) == -1) {
                        //console.log(cat)
                        api.addCategory(cat, data.data.id, () => {}, store.getters.token);
                    }
                })
                store.dispatch('updateAlbums');
                router.push(`/show/${payload.album.info.id}`);
            }, store.getters.token);
        },

        //photo management
        addPhotos(store, payload) {
            const no_of_photos = payload.photos.length
            payload.photos.forEach(file => {
                let category = payload.album.categories.filter(x => x.category.localeCompare(payload.category) == 0)[0];
                api.addPhotos(file, payload.album.info.id, 
                    category.id, 
                    data => {
                        store.commit('updatePhoto', {
                            category: category,
                            photo: data.data
                        });
                        //console.log(data)
                        if (file == payload.photos[no_of_photos - 1]) {
                            alert('success')
                            router.push(`/show/${payload.album.info.id}`);
                    }
                }, store.getters.token);
            })
        },

        addCoverPhoto(store, payload) {
            api.addCoverPhoto(payload.photo, payload.id, data => {
                //console.log('addCoverPhoto')
                store.dispatch('updateAlbums');
                router.push(`/show/${payload.id}`);
            }, store.getters.token);
        }
    }
});
