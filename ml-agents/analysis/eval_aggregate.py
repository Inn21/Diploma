from __future__ import annotations

import csv
import glob
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent
EVAL_DIR = ROOT / "out" / "eval"

SUCCESS_THRESHOLDS = (1, 10, 50)


def iqm(x: np.ndarray) -> float:
    if x.size == 0:
        return float("nan")
    q1, q3 = np.percentile(x, [25, 75])
    mid = x[(x >= q1) & (x <= q3)]
    return float(mid.mean()) if mid.size else float("nan")


def iqr(x: np.ndarray) -> float:
    if x.size == 0:
        return float("nan")
    q1, q3 = np.percentile(x, [25, 75])
    return float(q3 - q1)


def aggregate(csv_path: Path) -> dict:
    returns, scores = [], []
    with csv_path.open("r", encoding="utf-8") as f:
        r = csv.DictReader(f)
        model_name = ""
        for row in r:
            model_name = row["model"]
            returns.append(float(row["return"]))
            scores.append(int(row["score"]))

    returns_a = np.asarray(returns, dtype=np.float64)
    scores_a = np.asarray(scores, dtype=np.int64)
    n = returns_a.size

    out = {
        "model": model_name,
        "file": csv_path.name,
        "n": n,
        "G_mean": float(returns_a.mean()) if n else float("nan"),
        "G_median": float(np.median(returns_a)) if n else float("nan"),
        "IQM": iqm(returns_a),
        "IQR": iqr(returns_a),
        "score_mean": float(scores_a.mean()) if n else float("nan"),
        "score_median": float(np.median(scores_a)) if n else float("nan"),
        "score_max": int(scores_a.max()) if n else 0,
    }
    for t in SUCCESS_THRESHOLDS:
        out[f"p_succ_ge{t}"] = float((scores_a >= t).mean()) if n else float("nan")
    return out


def main() -> None:
    EVAL_DIR.mkdir(parents=True, exist_ok=True)
    files = sorted(glob.glob(str(EVAL_DIR / "eval_*.csv")))
    if not files:
        print(f"no eval_*.csv files in {EVAL_DIR}")
        return

    # keep newest per model
    latest: dict[str, Path] = {}
    for p in files:
        path = Path(p)
        # filename: eval_<name>_<timestamp>.csv  -> model name everything between first eval_ and last _YYYYMMDD
        stem = path.stem  # eval_FlappyBirdVector8_20260607_120000
        parts = stem.split("_")
        if len(parts) < 3:
            continue
        model = "_".join(parts[1:-2])
        if model not in latest or path.stat().st_mtime > latest[model].stat().st_mtime:
            latest[model] = path

    rows = [aggregate(p) for p in latest.values()]
    rows.sort(key=lambda r: r["model"])

    summary_csv = EVAL_DIR / "summary.csv"
    with summary_csv.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print(f"wrote {summary_csv}\n")

    hdr = (
        f"{'model':24s} {'n':>4s} {'G_mean':>9s} {'G_med':>8s} {'IQM':>8s} "
        f"{'IQR':>8s} {'score_med':>10s} {'score_max':>10s} "
        + " ".join(f"{'p>=' + str(t):>8s}" for t in SUCCESS_THRESHOLDS)
    )
    print(hdr)
    print("-" * len(hdr))
    for r in rows:
        line = (
            f"{r['model']:24s} {r['n']:>4d} {r['G_mean']:>9.3f} {r['G_median']:>8.3f} "
            f"{r['IQM']:>8.3f} {r['IQR']:>8.3f} {r['score_median']:>10.1f} {r['score_max']:>10d} "
            + " ".join(f"{r['p_succ_ge'+str(t)]:>8.2%}" for t in SUCCESS_THRESHOLDS)
        )
        print(line)


if __name__ == "__main__":
    main()
