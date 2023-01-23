#!/bin/sh
bin_name="elffycore"
proj_dir="${bin_name}/"
out_dir="${proj_dir}output/"
declare -A bins
bins=(
    ['x86_64-pc-windows-msvc']="${bin_name}.dll"
)

for target in ${!bins[@]}
do
    cargo build --target $target --manifest-path "${proj_dir}Cargo.toml"
    target_outdir="${out_dir}${target}/"
    mkdir -p $target_outdir
    cp "${proj_dir}target/${target}/debug/${bins[$target]}" $target_outdir
done
