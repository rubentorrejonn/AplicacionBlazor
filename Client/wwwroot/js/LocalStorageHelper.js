window.storageHelper = {
    save: function (key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    },
    load: function (key) {
        const data = localStorage.getItem(key);
        return data ? JSON.parse(data) : null;
    },
    remove: function (key) {
        localStorage.removeItem(key);
    }
};
