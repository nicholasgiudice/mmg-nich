// Filtro da busca (T-071). Sem JavaScript a lista inteira continua visível.
(function () {
  var q = document.getElementById("q");
  var items = document.querySelectorAll("#results li");
  if (!q) return;
  function norm(s) { return s.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, ""); }
  var keys = Array.prototype.map.call(items, function (li) { return norm(li.getAttribute("data-s") || ""); });
  q.addEventListener("input", function () {
    var t = norm(q.value.trim());
    items.forEach(function (li, i) { li.hidden = t !== "" && keys[i].indexOf(t) < 0; });
  });
})();
