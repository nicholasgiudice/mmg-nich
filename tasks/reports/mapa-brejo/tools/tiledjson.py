"""Escreve um .tmj no formato do Tiled usado no repositorio (indent 1, camadas de tiles com uma linha de mapa por linha)."""
import json
import re


def dumps(m):
    width = m["width"]
    marker = {}
    for i, l in enumerate(_layers(m["layers"])):
        if l["type"] == "tilelayer":
            key = f"@@DATA{i}@@"
            marker[key] = l["data"]
            l["data"] = key
    text = json.dumps(m, indent=1, ensure_ascii=False)
    for i, l in enumerate(_layers(m["layers"])):
        if l["type"] == "tilelayer" and isinstance(l["data"], str):
            l["data"] = marker[l["data"]]

    def repl(mo):
        prefix, key = mo.group(1), mo.group(2)
        data = marker[key]
        w = width
        rows = [", ".join(str(v) for v in data[r * w:(r + 1) * w]) for r in range(len(data) // w)]
        pad = " " * (len(prefix) + 1)
        return prefix + "[" + (",\n" + pad).join(rows) + "]"

    return re.sub(r'^( *"data": )"(@@DATA\d+@@)"', repl, text, flags=re.M) + "\n"


def _layers(ls):
    for l in ls:
        if l.get("type") == "group":
            yield from _layers(l["layers"])
        else:
            yield l
