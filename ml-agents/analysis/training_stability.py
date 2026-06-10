from __future__ import annotations

import csv
import sys
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
COMMON_GRID = 500
SECOND_HALF_FRAC = 0.50
PLOT_START_STEP = 5e5

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

SNAKE_GROUPS: dict[str, list[tuple[str, str]]] = {
    "Snake Vector PPO": [
        ("snake_vector_s8",  "SnakeVector"),
        ("snake_vector_s11", "SnakeVector"),
        ("snake_vector_s27", "SnakeVector"),
    ],
    "Snake Visual PPO": [
        ("snake_visual_s8",  "SnakeVisual"),
        ("snake_visual_s11", "SnakeVisual"),
        ("snake_visual_s27", "SnakeVisual"),
    ],
}

DATASET = sys.argv[1] if len(sys.argv) > 1 else "flappy"
PREFIX = "snake_" if DATASET == "snake" else ""
if DATASET == "snake":
    GROUPS = SNAKE_GROUPS


def load_reward(behavior_dir: Path) -> tuple[np.ndarray, np.ndarray]:
    ea = EventAccumulator(str(behavior_dir), size_guidance={"scalars": 0})
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


def main() -> None:
    rows = []
    fig, ax = plt.subplots(figsize=(8, 5))
    colors = plt.cm.tab10(np.linspace(0, 1, len(GROUPS)))

    for color, (label, runs) in zip(colors, GROUPS.items()):
        smoothed = []
        max_step = 0.0
        print(f"\n[{label}]")
        for run_name, behavior in runs:
            behavior_dir = RESULTS / run_name / behavior
            if not behavior_dir.exists():
                print(f"  ! skip {run_name}/{behavior}")
                continue
            steps, vals = load_reward(behavior_dir)
            smoothed.append((steps, ema(vals, EMA_ALPHA)))
            max_step = max(max_step, steps[-1])

        if not smoothed:
            continue

        grid = np.linspace(0, max_step, COMMON_GRID)
        stacked = np.vstack([np.interp(grid, s, v) for s, v in smoothed])
        mu_t = stacked.mean(axis=0)
        sigma_t = stacked.std(axis=0)

        tail_mask = grid >= max_step * (1 - ASYMPTOTE_FRAC)
        r_inf = float(mu_t[tail_mask].mean())
        sigma_inter_final = float(sigma_t[tail_mask].mean())
        cv_final = sigma_inter_final / r_inf if r_inf > 0 else float("nan")

        half_mask = grid >= max_step * SECOND_HALF_FRAC
        with np.errstate(divide="ignore", invalid="ignore"):
            cv_t = np.where(mu_t > 1e-6, sigma_t / mu_t, np.nan)
        cv_mean_half = float(np.nanmean(cv_t[half_mask]))
        cv_max = float(np.nanmax(cv_t[half_mask]))

        print(f"  R_inf = {r_inf:.1f}")
        print(f"  CV final (tail 10%)        = {cv_final*100:5.2f}%")
        print(f"  CV mean   (second half)    = {cv_mean_half*100:5.2f}%")
        print(f"  CV max    (second half)    = {cv_max*100:5.2f}%")

        plot_mask = grid >= PLOT_START_STEP
        ax.plot(grid[plot_mask], cv_t[plot_mask] * 100,
                color=color, label=label, linewidth=1.8)

        rows.append({
            "model": label,
            "n_seeds": stacked.shape[0],
            "R_inf": r_inf,
            "CV_final_pct": cv_final * 100,
            "CV_mean_half_pct": cv_mean_half * 100,
            "CV_max_half_pct": cv_max * 100,
        })

    ax.axvline(max_step * SECOND_HALF_FRAC, color="gray", linestyle=":", linewidth=0.8)
    ax.set_xlabel("Environment steps")
    ax.set_ylabel("Inter-seed coefficient of variation, σ/μ (%)")
    ax.set_title("Розходження кривих навчання між seed-ами")
    ax.legend(loc="upper right", frameon=False)
    ax.grid(alpha=0.3)
    ax.set_xlim(left=PLOT_START_STEP)
    ax.set_ylim(bottom=0)
    fig.tight_layout()

    plot_path = OUT / f"{PREFIX}stability_cv_curve.png"
    fig.savefig(plot_path, dpi=160)
    print(f"\nsaved {plot_path}")

    csv_path = OUT / f"{PREFIX}training_stability.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print(f"saved {csv_path}")

    print("\n=== Summary ===")
    print(f"{'Model':22s}  {'R_inf':>8s}  {'CV final':>10s}  "
          f"{'CV mean (2nd half)':>20s}  {'CV max (2nd half)':>18s}")
    for r in rows:
        print(f"{r['model']:22s}  {r['R_inf']:>8.1f}"
              f"  {r['CV_final_pct']:>9.2f}%"
              f"  {r['CV_mean_half_pct']:>18.2f}%"
              f"  {r['CV_max_half_pct']:>16.2f}%")


if __name__ == "__main__":
    main()
