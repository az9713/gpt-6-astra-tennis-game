mergeInto(LibraryManager.library, {
  PreflightReport: function(pointer) {
    var json = UTF8ToString(pointer);
    var receipt = JSON.parse(json);
    var panel = document.getElementById('preflight-status');
    if (!panel) {
      panel = document.createElement('div');
      panel.id = 'preflight-status';
      panel.setAttribute('role', 'status');
      panel.style.cssText = 'position:fixed;bottom:0;left:0;right:0;background:#112833;color:#e3ffff;padding:8px;font:14px sans-serif;z-index:100';
      document.body.appendChild(panel);
    }
    panel.textContent = receipt.status + ' | Skin motion: ' + receipt.animatedVertexDelta.toFixed(3) + ' m | Interactions: ' + receipt.interactions;
    panel.setAttribute('data-receipt', json);
  }
});
