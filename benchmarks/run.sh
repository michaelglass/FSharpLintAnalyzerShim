#!/usr/bin/env bash
# Benchmark: FSharpLint CLI vs the shim (invoked via an in-process runner that
# mirrors the fsharp-analyzers + shim code path).
set -euo pipefail

cd "$(dirname "$0")/.."

RUNNER_DLL="benchmarks/BenchmarkRunner/bin/Release/net10.0/BenchmarkRunner.dll"

SAMPLE_CONFIG="benchmarks/SampleProject/fsharplint.json"
SAMPLE_PROJECT="benchmarks/SampleProject/SampleProject.fsproj"

FSHW_ROOT="../FsHotWatch"
FSHW_CONFIG="$FSHW_ROOT/fsharplint.json"
FSHW_PROJECT="$FSHW_ROOT/src/FsHotWatch/FsHotWatch.fsproj"
FSHW_SOLUTION="$FSHW_ROOT/FsHotWatch.slnx"

RESULTS_SAMPLE="benchmarks/results-sample.md"
RESULTS_FSHW_PROJECT="benchmarks/results-fshotwatch-project.md"
RESULTS_FSHW_SOLUTION="benchmarks/results-fshotwatch-solution.md"

echo "==> Building shim + runner + SampleProject in Release"
dotnet build -c Release --nologo -v q >/dev/null
dotnet build benchmarks/BenchmarkRunner -c Release --nologo -v q >/dev/null
dotnet build "$SAMPLE_PROJECT" -c Release --nologo -v q >/dev/null

echo "==> Ensuring tools are restored"
dotnet tool restore >/dev/null

if [[ -d "$FSHW_ROOT" ]]; then
    echo "==> Building FsHotWatch (sibling checkout at $FSHW_ROOT) in Release"
    (cd "$FSHW_ROOT" && dotnet build -c Release --nologo -v q >/dev/null)
else
    echo "==> FsHotWatch checkout not found at $FSHW_ROOT -- skipping its benchmarks"
fi

run_bench() {
    local label="$1"
    local config="$2"
    local target="$3"
    local out="$4"

    echo
    echo "==> Benchmark: $label"
    hyperfine \
        --warmup 1 \
        --runs 5 \
        --ignore-failure \
        --export-markdown "$out" \
        --command-name "fsharplint CLI" \
            "dotnet fsharplint lint -l $config $target" \
        --command-name "shim (in-process runner)" \
            "dotnet $RUNNER_DLL $target"
}

run_bench \
    "SampleProject (controlled fixture, 9 files)" \
    "$SAMPLE_CONFIG" \
    "$SAMPLE_PROJECT" \
    "$RESULTS_SAMPLE"

if [[ -d "$FSHW_ROOT" ]]; then
    run_bench \
        "FsHotWatch core (single project)" \
        "$FSHW_CONFIG" \
        "$FSHW_PROJECT" \
        "$RESULTS_FSHW_PROJECT"

    run_bench \
        "FsHotWatch (full solution, 12 nested projects)" \
        "$FSHW_CONFIG" \
        "$FSHW_SOLUTION" \
        "$RESULTS_FSHW_SOLUTION"
fi

echo
echo "==> All results:"
for f in "$RESULTS_SAMPLE" "$RESULTS_FSHW_PROJECT" "$RESULTS_FSHW_SOLUTION"; do
    if [[ -f "$f" ]]; then
        echo
        echo "### $f"
        cat "$f"
    fi
done
