const api = {
    base: 'http://billowy-fluttering-enemy.glitch.me',
    send(api, resolve, reject, tkn, body) {
        const xhr = new XMLHttpRequest();
        if (body) {
            xhr.open('POST', this.base + api);
            //xhr.setRequestHeader('Content-Type', 'multipart/form-data');
        } else {
            xhr.open('GET', this.base + api);
        }
        if (tkn) {
            xhr.setRequestHeader('Authorization', ` ${tkn}`);
        }
  
        xhr.onreadystatechange = function () {
                if (xhr.readyState === 4) {
                const obj = JSON.parse(this.response);
                return obj.ok ? resolve(obj)
                    : (reject || console.log)(obj);
            }
        };
        
        //console.log(api);
        if (body instanceof FormData) {
            xhr.send(body);
        } else {
            xhr.send(JSON.stringify(body));
        }
    },
    
    // login management
    enterEmail(email, func) {
        this.send(`/login?email=${email}`, func, () => { alert('Such email is not registered ') });
    },

    logIn(tkn, pass, func) {
        this.send(`/login?tkn=${tkn}&pass=${pass}`, func, () => { alert('Wrong token entered') });
    },

    // album management
    createAlbum(name, func, tkn) {
        this.send(`/albums/new?name=${name}`, func, () => { alert('unsuccessful') }, tkn);
    },

    getAlbums(func, tkn) {
        this.send('/albums/list', func, () => { alert('unsuccessful' )}, tkn);
    },

    deleteAlbum(id, func, tkn) {
        this.send(`/albums/del?aid=${id}`, func, () => { alert('unsuccessful' )}, tkn);
    },

    updateAlbum(name, id, func, tkn) {
        this.send(`/albums/edit?aid=${id}&name=${name}`, func, () => { alert(' updateAlbum unsuccessful') }, tkn);
    },

    // category management
    getCategories(id, func, tkn) {
        this.send(`/albums/cats?aid=${id}`, func, (e) => { alert('get category unsuccessful') }, tkn);
    },

    addCategory(name, id, func, tkn) {
        this.send(`/albums/cadd?aid=${id}&category=${name}`, func, (e) => { console.log(e) }, tkn);
    },

    editCategory(name, id, func, tkn) {
        this.send(`/albums/cedit?cid=${id}&category=${name}`, func, (e) => { alert(e) }, tkn);
    },

    deleteCategory(id, func, tkn) {
        this.send(`/albums/cdel?cid=${id}`, func, (e) => { alert(e) }, tkn);
    },

    // photos management
    getPhotos(cid, func, tkn) {
        this.send(`/photos/listcat?cid=${cid}`, func, () => { alert('get photos unsuccessful')}, tkn);
    },

    getCoverPhoto(aid, func, tkn) {
        this.send(`/photos/listcat?aid=${aid}`, func, (e) => { 
            alert('get cover photos unsuccessful') ;
            console.log(e);
        }, tkn);
    },

    addPhotos(img, aid, cid, func, tkn) {
        var formData = new FormData();
        // HTML file input
        formData.append("img", img, img.name);
        this.send(`/photos/new?aid=${aid}&cid=${cid}`, func, (e) => { console.log(e) }, tkn, formData);
    },

    addCoverPhoto(img, aid, func, tkn) {
        var formData = new FormData();
        formData.append("img", img, img.name);
        this.send(`/photos/new?aid=${aid}`, func, (e) => { console.log(e) }, tkn, formData);
    }
}