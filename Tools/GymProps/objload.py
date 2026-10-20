"""Gym pack -OBJ:n lataus (numpy): kärjet, UV:t, normaalit, kolmiot; komponentit (yhtenäiset osat) sijainnin mukaan yhdistetyistä kärjistä."""
import numpy as np

def load_obj(path):
    V, VT, VN, F = [], [], [], []
    for line in open(path, encoding="utf8", errors="ignore"):
        if line.startswith("v "): V.append(list(map(float, line.split()[1:4])))
        elif line.startswith("vt "): VT.append(list(map(float, line.split()[1:3])))
        elif line.startswith("vn "): VN.append(list(map(float, line.split()[1:4])))
        elif line.startswith("f "):
            idx = []
            for tok in line.split()[1:]:
                p = tok.split("/"); idx.append((int(p[0]) - 1, int(p[1]) - 1 if len(p) > 1 and p[1] else -1, int(p[2]) - 1 if len(p) > 2 and p[2] else -1))
            for k in range(1, len(idx) - 1): F.append((idx[0], idx[k], idx[k + 1]))
    return np.array(V, np.float64), np.array(VT, np.float64).reshape(-1, 2), np.array(VN, np.float64).reshape(-1, 3), F

def components(V, F):
    key = {}
    weld = np.zeros(len(V), np.int64)
    for i, v in enumerate(V):
        k = tuple(np.round(v, 3)); weld[i] = key.setdefault(k, len(key))
    parent = list(range(len(key)))
    def find(a):
        while parent[a] != a: parent[a] = parent[parent[a]]; a = parent[a]
        return a
    for t in F:
        a, b, c = (weld[t[0][0]], weld[t[1][0]], weld[t[2][0]])
        ra, rb, rc = find(a), find(b), find(c); parent[rb] = ra; parent[find(rc)] = ra
    comp = np.array([find(weld[t[0][0]]) for t in F])
    return comp
