#!/bin/sh

build_target='x86_64-pc-windows-msvc'

cargo build --release --target $build_target --manifest-path elffycore/Cargo.toml
