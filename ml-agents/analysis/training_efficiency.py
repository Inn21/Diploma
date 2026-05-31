from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import matplotlib.pyplot as plt
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "results"
OUT = Path(__file__).resolve().parent / "out"
OUT.mkdir(exist_ok=True)

REWARD_TAG = "Environment/Cumulative Reward"
EMA_ALPHA = 0.05
ASYMPTOTE_FRAC = 0.10
THRESHOLD_FRAC = 0.90
COMMON_GRID = 500

GROUPS: dict[str, list[tuple[str, str]]] = {
    "Vector PPO": [
        ("vector_s8",  "FlappyBirdVector"),
        ("vector_s11", "FlappyBirdVector"),
        ("vector_s27", "FlappyBirdVector"),
    ],
    "Visual PPO (48x84)": [
        ("vector_s8",  "FlappyBird"),
        ("vector_s11", "FlappyBird"),
        ("vector_s27", "FlappyBird"),
    ],
}


def load_reward(behavior_dir: Path) -> tuple[np.ndarray, np.ndarray]:
    ea = EventAccumulator(str(behavior_dir),
                          size_guidance={"scalars": 0})
    ea.Reload()
    events = ea.Scalars(REWARD_TAG)
    steps = np.array([e.step for e in events], dtype=np.float64)
    vals = np.array([e.value for e in events], dtype=np.float64)
    return steps, vals


def ema(x: np.ndarray, alpha: float) -> np.ndarray:
    out = np.empty_like(x)
    out[0] = x[0]
    for i in range(1, len(x)):
        out[i] = alpha * x[i] + (1 - alpha) * out[i - 1]
    return out


def resample(steps: np.ndarray, vals: np.ndarray,
             grid: np.ndarray) -> np.ndarray:
    return np.interp(grid, steps, vals)


@dataclass
class RunMetrics:
    r_inf: float
    n90: float
    auc: float


def metrics_for_run(steps: np.ndarray, vals: np.ndarray) -> RunMetrics:
    smooth = ema(vals, EMA_ALPHA)
    total = steps[-1]
    tail_mask = steps >= total * (1 - ASYMPTOTE_FRAC)
    r_inf = float(np.mean(smooth[tail_mask]))
    threshold = THRESHOLD_FRAC * r_inf
    baseline = float(smooth[0])
    if r_inf <= baseline:
        n90 = float("nan")
    else:
        idx = np.argmax(smooth >= threshold)
        n90 = float(steps[idx]) if smooth[idx] >= threshold else float("nan")
    if r_inf > baseline:
        norm = np.clip((smooth - baseline) / (r_inf - baseline), 0.0, 1.0)
        auc = float(np.trapz(norm, steps) / (steps[-1] - steps[0]))
    else:
        auc = float("nan")
    return RunMetrics(r_inf=r_inf, n90=n90, auc=auc)


def main() -> None:
    rows = []
    fig, ax = plt.subplots(figsize=(8, 5))
    colors = plt.cm.tab10(np.linspace(0, 1, len(GROUPS)))

    for color, (label, runs) in zip(colors, GROUPS.items()):
        per_run = []
        max_step = 0.0
        print(f"\n[{label}]")
        for run_name, behavior in runs:
            behavior_dir = RESULTS / run_name / behavior
            if not behavior_dir.exists():
                print(f"  ! skip {run_name}/{behavior}: dir missing")
                continue
            steps, vals = load_reward(behavior_dir)
            m = metrics_for_run(steps, vals)
            per_run.append((run_name, steps, vals, m))
            max_step = max(max_step, steps[-1])
            tag = f"{run_name}/{behavior}"
            n90_s = f"{m.n90:>10.0f}" if not np.isnan(m.n90) else "       n/a"
            print(f"  {tag:35s}  R_inf={m.r_inf:7.2f}  "
                  f"N90={n90_s}  AUC={m.auc:.3f}")

        if not per_run:
            continue

        grid = np.linspace(0, max_step, COMMON_GRID)
        stacked = np.vstack([
            resample(s, ema(v, EMA_ALPHA), grid) for _, s, v, _ in per_run
        ])
        mean = stacked.mean(axis=0)
        std = stacked.std(axis=0)
        ax.plot(grid, mean, color=color, label=label, linewidth=1.8)
        ax.fill_between(grid, mean - std, mean + std,
                        color=color, alpha=0.18)

        r_infs = np.array([m.r_inf for _, _, _, m in per_run])
        n90s = np.array([m.n90 for _, _, _, m in per_run])
        aucs = np.array([m.auc for _, _, _, m in per_run])
        ax.axhline(THRESHOLD_FRAC * r_infs.mean(),
                   color=color, linestyle=":", linewidth=0.8, alpha=0.6)

        rows.append({
            "model": label,
            "n_seeds": len(per_run),
            "R_inf_mean": r_infs.mean(),
            "R_inf_std": r_infs.std(),
            "N90_mean": np.nanmean(n90s),
            "N90_std": np.nanstd(n90s),
            "AUC_mean": np.nanmean(aucs),
            "AUC_std": np.nanstd(aucs),
        })

    ax.set_xlabel("Environment steps")
    ax.set_ylabel("Cumulative reward (EMA, α=0.05)")
    ax.set_title("Learning curves (mean ± std over seeds)")
    ax.legend(loc="lower right", frameon=False)
    ax.grid(alpha=0.3)
    fig.tight_layout()

    plot_path = OUT / "learning_curves.png"
    fig.savefig(plot_path, dpi=160)
    print(f"\nsaved {plot_path}")

    csv_path = OUT / "training_efficiency.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print(f"saved {csv_path}")

    print("\n=== Summary ===")
    print(f"{'Model':28s}  {'seeds':>5s}  {'R_inf':>14s}"
          f"  {'N90 (steps)':>18s}  {'AUC':>12s}")
    for r in rows:
        print(f"{r['model']:28s}  {r['n_seeds']:>5d}"
              f"  {r['R_inf_mean']:>6.2f} ± {r['R_inf_std']:>4.2f}"
              f"  {r['N90_mean']:>9.0f} ± {r['N90_std']:>6.0f}"
              f"  {r['AUC_mean']:>5.3f} ± {r['AUC_std']:>4.3f}")


if __name__ == "__main__":
    main()
