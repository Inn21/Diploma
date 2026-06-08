from __future__ import annotations

import csv
import json
import os
from pathlib import Path

import numpy as np
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "results"
OUT = Path(__file__).resolve().parent / "out"
OUT.mkdir(exist_ok=True)

REWARD_TAG = "Environment/Cumulative Reward"

GROUPS: dict[str, list[tuple[str, str, str]]] = {
    "Vector PPO": [
        ("vector_28-05-26", "FlappyBirdVector", "FlappyBirdVector"),
    ],
    "Visual PPO (48x84)": [
        ("vector_s8",  "FlappyBird", "FlappyBird"),
        ("vector_s11", "FlappyBird", "FlappyBird"),
        ("vector_s27", "FlappyBird", "FlappyBird"),
    ],
}


def last_step(behavior_dir: Path) -> float:
    ea = EventAccumulator(str(behavior_dir), size_guidance={"scalars": 0})
    ea.Reload()
    events = ea.Scalars(REWARD_TAG)
    return float(events[-1].step)


def timer_data(run_dir: Path) -> dict:
    p = run_dir / "run_logs" / "timers.json"
    d = json.load(open(p))
    adv = d["children"]["TrainerController.start_learning"]["children"]["TrainerController.advance"]
    return {
        "total_min": d["total"] / 60,
        "env_step_min": adv["children"].get("env_step", {}).get("total", 0) / 60,
        "trainer_min": adv["children"].get("trainer_advance", {}).get("total", 0) / 60,
        "advances": adv["count"],
    }


def main() -> None:
    rows = []
    for label, runs in GROUPS.items():
        per_run = []
        print(f"\n[{label}]")
        for run_name, behavior, onnx_name in runs:
            run_dir = RESULTS / run_name
            onnx_path = run_dir / f"{onnx_name}.onnx"
            behavior_dir = run_dir / behavior
            t = timer_data(run_dir)
            steps = last_step(behavior_dir)
            onnx_kb = os.path.getsize(onnx_path) / 1024
            steps_per_sec = steps / (t["total_min"] * 60)
            per_run.append({
                "run": run_name,
                "total_min": t["total_min"],
                "env_step_min": t["env_step_min"],
                "trainer_min": t["trainer_min"],
                "steps": steps,
                "steps_per_sec": steps_per_sec,
                "onnx_kb": onnx_kb,
            })
            print(f"  {run_name:25s}  total={t['total_min']:6.1f}min  "
                  f"env={t['env_step_min']:6.1f}min  train={t['trainer_min']:6.1f}min  "
                  f"steps={steps/1e6:.2f}M  rate={steps_per_sec:6.0f}/s  onnx={onnx_kb:6.1f}KB")

        if not per_run:
            continue

        agg = {
            "model": label,
            "n_runs": len(per_run),
            "total_min_mean": np.mean([r["total_min"] for r in per_run]),
            "total_min_std":  np.std([r["total_min"] for r in per_run]),
            "env_step_pct":   np.mean([r["env_step_min"]/r["total_min"]*100 for r in per_run]),
            "trainer_pct":    np.mean([r["trainer_min"]/r["total_min"]*100 for r in per_run]),
            "steps_M_mean":   np.mean([r["steps"] for r in per_run]) / 1e6,
            "steps_per_sec":  np.mean([r["steps_per_sec"] for r in per_run]),
            "onnx_kb":        np.mean([r["onnx_kb"] for r in per_run]),
        }
        rows.append(agg)

    csv_path = OUT / "compute_resources.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print(f"\nsaved {csv_path}")

    print("\n=== Summary ===")
    print(f"{'Model':22s}  {'wall-clock':>14s}  {'env%':>6s}  {'train%':>7s}  "
          f"{'steps':>7s}  {'steps/sec':>10s}  {'onnx':>9s}")
    for r in rows:
        print(f"{r['model']:22s}"
              f"  {r['total_min_mean']:>7.1f} ± {r['total_min_std']:<4.1f}min"
              f"  {r['env_step_pct']:>5.1f}%"
              f"  {r['trainer_pct']:>6.1f}%"
              f"  {r['steps_M_mean']:>5.2f}M"
              f"  {r['steps_per_sec']:>9.0f}/s"
              f"  {r['onnx_kb']:>7.1f}KB")


if __name__ == "__main__":
    main()
