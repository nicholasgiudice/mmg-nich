// Página de download: lê /downloads/latest.json (escrito por scripts/deploy-download.sh) e mostra versão, tamanho e data.
// Sem JavaScript, ou sem o arquivo, o botão continua apontando para /downloads/Valdran-windows.zip.
(function () {
  var link = document.querySelector('[data-dl="link"]');
  var meta = document.querySelector('[data-dl="meta"]');
  var sha = document.querySelector('[data-dl="sha"]');
  var unavailable = document.querySelector('[data-dl="unavailable"]');
  if (!link || !window.fetch) return;

  function size(bytes) {
    return (bytes / (1024 * 1024)).toLocaleString("pt-BR", { maximumFractionDigits: 1 }) + " MB";
  }

  function missing() {
    link.hidden = true;
    unavailable.hidden = false;
  }

  fetch("/downloads/latest.json", { cache: "no-cache" })
    .then(function (r) { return r.ok ? r.json() : null; })
    .then(function (info) {
      if (!info || !info.file) return missing();
      link.href = "/downloads/" + encodeURIComponent(info.file);
      var parts = ["Versão " + info.version];
      if (info.size) parts.push(size(info.size));
      if (info.date) parts.push("publicada em " + new Date(info.date).toLocaleDateString("pt-BR"));
      meta.textContent = parts.join(" · ");
      if (info.sha256) {
        sha.textContent = "SHA-256: " + info.sha256;
        sha.hidden = false;
      }
    })
    .catch(function () { /* fora do servidor (arquivo local): deixa o botão como está */ });
})();
