"""Summarize ReplayAudit output; never infer a hard replication radius from successes."""
import csv
import json
import sys
from pathlib import Path

folder = Path(sys.argv[1])
rows = [json.loads(p.read_text(encoding="utf-8")) for p in sorted(folder.glob("*.json")) if p.name != "summary.json"]
observations = [dict(Replay=r["Match"]["ReplayFile"], **o) for r in rows for o in r["Observations"]]
valid = [o for o in observations if o["BothAlive"] and o["ObservedTransition"] and o["DistanceMeters"] is not None]
valid.sort(key=lambda o: o["DistanceMeters"], reverse=True)
with (folder / "observations.csv").open("w", newline="", encoding="utf-8-sig") as f:
    writer = csv.DictWriter(f, fieldnames=list(observations[0]) if observations else [])
    writer.writeheader()
    writer.writerows(observations)
summary = dict(Files=len(rows), Errors=len(list(folder.glob("*.error"))), WithTeammates=sum(bool(r["Match"]["Teammates"]) for r in rows), Observations=len(observations), Eligible=len(valid), MaximumObservedMeters=valid[0]["DistanceMeters"] if valid else None, Top=valid[:10])
(folder / "summary.json").write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8")
print(json.dumps(summary, indent=2, ensure_ascii=False))
