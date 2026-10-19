mergeInto(LibraryManager.library, {
  // PATCHes /api/game/profile with the session cookie, then reports the HTTP status (0 = network error) to C#.
  WebBackend_Patch: function (bodyPtr, requestId, callback) {
    var body = UTF8ToString(bodyPtr);
    fetch('/api/game/profile', {
      method: 'PATCH',
      credentials: 'same-origin',
      keepalive: true,
      headers: { 'Content-Type': 'application/json' },
      body: body
    })
      .then(function (response) { return response.status; }, function () { return 0; })
      .then(function (status) { {{{ makeDynCall('vii', 'callback') }}}(requestId, status); });
  }
});
