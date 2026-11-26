mergeInto(LibraryManager.library, {
    CoinRush_GetOrStoreToken: function (candidate, output, capacity) {
        try {
            var key = "coin-rush-participant-v1";
            var token = window.sessionStorage.getItem(key);
            if (!token || !/^[0-9a-f]{32}$/.test(token)) {
                token = UTF8ToString(candidate);
                window.sessionStorage.setItem(key, token);
            }

            stringToUTF8(token, output, capacity);
            return 1;
        } catch (_) {
            return 0;
        }
    }
});
