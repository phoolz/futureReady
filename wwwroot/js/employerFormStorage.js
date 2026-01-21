window.employerFormStorage = {
    getKey: function (token) {
        return 'employerForm_' + token;
    },
    save: function (token, data) {
        try {
            localStorage.setItem(this.getKey(token), data);
            return true;
        } catch (e) {
            console.error('Error saving to localStorage:', e);
            return false;
        }
    },
    load: function (token) {
        try {
            return localStorage.getItem(this.getKey(token));
        } catch (e) {
            console.error('Error loading from localStorage:', e);
            return null;
        }
    },
    clear: function (token) {
        try {
            localStorage.removeItem(this.getKey(token));
            return true;
        } catch (e) {
            console.error('Error clearing localStorage:', e);
            return false;
        }
    }
};
